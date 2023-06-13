// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using EnsureThat;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Core;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Models.Common;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Blob.Features.Export;

internal sealed class AzureBlobExportSink : IExportSink
{
    public event EventHandler<CopyFailureEventArgs> CopyFailure;

    private readonly IFileStore _source;
    private readonly BlobContainerClient _dest;
    private readonly ConcurrentQueue<ExportErrorLogEntry> _errors;
    private readonly AzureBlobExportFormatOptions _output;
    private readonly BlobOperationOptions _blobOptions;
    private readonly JsonSerializerOptions _jsonOptions;

    private const int BlockSize = 2 * 1024 * 1024; // 2MB

    public AzureBlobExportSink(
        IFileStore source,
        BlobContainerClient dest,
        AzureBlobExportFormatOptions outputOptions,
        BlobOperationOptions blobOptions,
        JsonSerializerOptions jsonOptions)
    {
        _source = EnsureArg.IsNotNull(source, nameof(source));
        _dest = EnsureArg.IsNotNull(dest, nameof(source));
        _output = EnsureArg.IsNotNull(outputOptions, nameof(outputOptions));
        _blobOptions = EnsureArg.IsNotNull(blobOptions, nameof(blobOptions));
        _jsonOptions = EnsureArg.IsNotNull(jsonOptions, nameof(jsonOptions));
        _errors = new ConcurrentQueue<ExportErrorLogEntry>();
    }

    public async Task<bool> CopyAsync(ReadResult value, CancellationToken cancellationToken = default)
    {
        // TODO: Use new blob SDK for copying block blobs when available
        if (value.Failure != null)
        {
            EnqueueError(value.Failure.Identifier, value.Failure.Exception.Message);
            return false;
        }

        using Stream sourceStream = await _source.GetStreamingFileAsync(value.Identifier.Version, value.Identifier.PartitionName, cancellationToken);
        BlobClient destBlob = _dest.GetBlobClient(_output.GetFilePath(value.Identifier));

        try
        {
            await destBlob.UploadAsync(sourceStream, new BlobUploadOptions { TransferOptions = _blobOptions.Upload }, cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is not RequestFailedException rfe || rfe.Status < 400 || rfe.Status >= 500) // Do not include client errors
        {
            CopyFailure?.Invoke(this, new CopyFailureEventArgs(value.Identifier, ex));
            EnqueueError(DicomIdentifier.ForInstance(value.Identifier), ex.Message);
            return false;
        }
    }

    public ValueTask DisposeAsync()
        => new ValueTask(FlushAsync());

    public async Task<Uri> InitializeAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Should we create the container if it's not present?
        try
        {
            if (!await _dest.ExistsAsync(cancellationToken))
            {
                throw new SinkInitializationFailureException(
                    string.Format(CultureInfo.CurrentCulture, DicomBlobResource.ContainerDoesNotExist, _dest.Name, _dest.AccountName));
            }

            AppendBlobClient client = _dest.GetAppendBlobClient(_output.ErrorFile);
            await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            return new Uri(client.Uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped), UriKind.Absolute);
        }
        catch (AggregateException ae) when (ae.InnerException is RequestFailedException)
        {
            throw new SinkInitializationFailureException(
                string.Format(CultureInfo.CurrentCulture, DicomBlobResource.BlobStorageConnectionFailure, _dest.Name, _dest.AccountName),
                ae);
        }
        catch (AuthenticationFailedException afe)
        {
            throw new SinkInitializationFailureException(
                string.Format(CultureInfo.CurrentCulture, DicomBlobResource.BlobStorageAuthenticateFailure, _dest.Name, _dest.AccountName),
                afe);
        }
        catch (RequestFailedException rfe)
        {
            throw new SinkInitializationFailureException(
                string.Format(CultureInfo.CurrentCulture, DicomBlobResource.BlobStorageRequestFailure, _dest.Name, _dest.AccountName),
                rfe);
        }
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        AppendBlobClient client = _dest.GetAppendBlobClient(_output.ErrorFile);

        using var buffer = new MemoryStream(BlockSize);
        while (!_errors.IsEmpty)
        {
            // Fill up the buffer
            buffer.SetLength(0);
            while (buffer.Position < BlockSize && _errors.TryDequeue(out ExportErrorLogEntry entry))
            {
                await JsonSerializer.SerializeAsync(buffer, entry, _jsonOptions, cancellationToken);
                buffer.WriteByte(10); // '\n' in UTF-8 for normalized line endings across platforms
            }

            // Append the block
            buffer.Seek(0, SeekOrigin.Begin);
            await client.AppendBlockAsync(buffer, cancellationToken: cancellationToken);
        }
    }

    private void EnqueueError(DicomIdentifier identifier, string message)
        => _errors.Enqueue(
            new ExportErrorLogEntry
            {
                Error = message,
                Identifier = identifier,
                Timestamp = Clock.UtcNow,
            });
}
