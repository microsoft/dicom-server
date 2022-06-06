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
using Microsoft.Extensions.Options;
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
    private readonly SemaphoreSlim _errorSemaphore;
    private bool _disposed;

    private const int BlockSize = 2 * 1024 * 1024; // 2MB

    public AzureBlobExportSink(
        IFileStore source,
        BlobContainerClient dest,
        IOptions<AzureBlobExportFormatOptions> outputOptions,
        IOptions<BlobOperationOptions> blobOptions,
        IOptions<JsonSerializerOptions> jsonOptions)
    {
        _source = EnsureArg.IsNotNull(source, nameof(source));
        _dest = EnsureArg.IsNotNull(dest, nameof(source));
        _output = EnsureArg.IsNotNull(outputOptions?.Value, nameof(outputOptions));
        _blobOptions = EnsureArg.IsNotNull(blobOptions?.Value, nameof(blobOptions));
        _jsonOptions = EnsureArg.IsNotNull(jsonOptions?.Value, nameof(jsonOptions));
        _errors = new ConcurrentQueue<ExportErrorLogEntry>();
        _errorSemaphore = new SemaphoreSlim(1, 1); // Only 1 writer for errors at a time
    }

    public Uri ErrorHref
    {
        get
        {
            // Add trailing '/' before concatenating to preserve the container
            string container = _dest.Uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped);
            if (container[^1] != '/')
                container += '/';

            return new Uri(new Uri(container, UriKind.Absolute), _output.ErrorFile);
        }
    }

    public async Task<bool> CopyAsync(ReadResult value, CancellationToken cancellationToken = default)
    {
        // TODO: Use new blob SDK for copying block blobs when available
        if (value.Failure != null)
        {
            EnqueueError(value.Failure.Identifier, value.Failure.Exception.Message);
            return false;
        }

        using Stream sourceStream = await _source.GetFileAsync(value.Identifier, cancellationToken);
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

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await _errorSemaphore.WaitAsync();
        try
        {
            if (_disposed)
                return;

            await FlushErrorsAsync();
            _disposed = true;
        }
        finally
        {
            _errorSemaphore.Release();
            _errorSemaphore.Dispose();
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Should we create the container if it's not present?
        try
        {
            if (!await _dest.ExistsAsync(cancellationToken))
                throw new SinkInitializationFailureException(
                    string.Format(CultureInfo.CurrentCulture, DicomBlobResource.ContainerDoesNotExist, _dest.Name, _dest.AccountName));

            AppendBlobClient client = _dest.GetAppendBlobClient(_output.ErrorFile);
            await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
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

    private async ValueTask FlushErrorsAsync(CancellationToken cancellationToken = default)
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
