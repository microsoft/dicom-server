// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Core;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Models.Common;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Blob.Features.Export;

internal sealed class AzureBlobExportSink : IExportSink
{
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

    public event EventHandler<CopyFailureEventArgs> CopyFailure;

    private readonly IFileStore _source;
    private readonly BlobContainerClient _dest;
    private readonly StreamWriter _errorWriter;
    private readonly AzureBlobExportFormatOptions _output;
    private readonly BlobOperationOptions _blobOptions;
    private readonly JsonSerializerOptions _jsonOptions;

    private const int BlockSize = 2 * 1024 * 1024;

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
        _errorWriter = new StreamWriter(new MemoryStream(BlockSize), _output.ErrorEncoding, leaveOpen: false);
    }

    public async Task<bool> CopyAsync(ReadResult value, CancellationToken cancellationToken = default)
    {
        // TODO: Use new blob SDK for copying block blobs when available
        if (value.Failure != null)
        {
            await WriteErrorAsync(value.Failure.Identifier, value.Failure.Exception.Message, cancellationToken);
            return false;
        }

        using Stream sourceStream = await _source.GetFileAsync(value.Identifier, cancellationToken);
        BlobClient destBlob = _dest.GetBlobClient(_output.GetFilePath(value.Identifier));

        try
        {
            await destBlob.UploadAsync(sourceStream, new BlobUploadOptions { TransferOptions = _blobOptions.Upload }, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            CopyFailure?.Invoke(this, new CopyFailureEventArgs(value.Identifier, ex));
            await WriteErrorAsync(DicomIdentifier.ForInstance(value.Identifier), ex.Message, cancellationToken);
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await FlushErrorsAsync();
        await _errorWriter.DisposeAsync();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Should we create the container if it's not present?
        if (!await _dest.ExistsAsync(cancellationToken))
            throw new IOException(
                string.Format(CultureInfo.CurrentCulture, DicomBlobResource.ContainerDoesNotExist, _dest.Name));

        AppendBlobClient client = _dest.GetAppendBlobClient(_output.ErrorFile);
        await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
    }

    internal async ValueTask FlushErrorsAsync(CancellationToken cancellationToken = default)
    {
        // Only flush if we have errors
        if (_errorWriter.BaseStream.Length > 0)
        {
            // Move the stream back to the beginning
            _errorWriter.BaseStream.Seek(0, SeekOrigin.Begin);

            // Append the errors
            AppendBlobClient client = _dest.GetAppendBlobClient(_output.ErrorFile);
            await client.AppendBlockAsync(_errorWriter.BaseStream, cancellationToken: cancellationToken);

            // Reset the stream
            _errorWriter.BaseStream.SetLength(0);
        }
    }

    private async ValueTask WriteErrorAsync(DicomIdentifier identifier, string message, CancellationToken cancellationToken = default)
    {
        await _errorWriter.WriteLineAsync(
            JsonSerializer.Serialize(
                new ExportErrorLogEntry
                {
                    Error = message,
                    Identifier = identifier,
                    Timestamp = Clock.UtcNow,
                },
                _jsonOptions));
        await _errorWriter.FlushAsync();

        if (_errorWriter.BaseStream.Position >= BlockSize)
            await FlushErrorsAsync(cancellationToken);
    }
}
