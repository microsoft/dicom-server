// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Core;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Blob.Features.Export;

internal sealed class AzureBlobExportSink : IExportSink
{
    public event EventHandler<CopyFailureEventArgs> CopyFailure;

    public Uri ErrorHref => _errorClient.Uri;

    private readonly BlobContainerClient _sourceClient;
    private readonly BlobOperationOptions _options;
    private readonly BlobContainerClient _destClient;
    private readonly AppendBlobClient _errorClient;
    private readonly StreamWriter _errorWriter;
    private readonly string _destinationPath;

    private const int BlockSize = 2 * 1024 * 1024;

    public AzureBlobExportSink(
        BlobServiceClient sourceClient,
        BlobContainerClient destClient,
        Encoding errorEncoding,
        string destinationPath,
        string errorBlobName,
        IOptionsMonitor<BlobContainerConfiguration> sourceContainerOptions,
        IOptions<BlobOperationOptions> operationOptions)
    {
        EnsureArg.IsNotNull(sourceClient, nameof(sourceClient));
        EnsureArg.IsNotNull(destClient, nameof(destClient));
        EnsureArg.IsNotNull(errorEncoding, nameof(errorEncoding));
        EnsureArg.IsNotNull(sourceContainerOptions, nameof(sourceContainerOptions));
        EnsureArg.IsNotNull(operationOptions?.Value, nameof(operationOptions));

        BlobContainerConfiguration containerConfiguration = sourceContainerOptions.Get(Constants.BlobContainerConfigurationName);
        _sourceClient = sourceClient.GetBlobContainerClient(containerConfiguration.ContainerName);
        _destClient = destClient;
        _errorClient = _sourceClient.GetAppendBlobClient(errorBlobName);
        _errorWriter = new StreamWriter(new MemoryStream(BlockSize), errorEncoding);
        _destinationPath = destinationPath;
        _options = operationOptions.Value;
    }

    public async Task<bool> CopyAsync(VersionedInstanceIdentifier identifier, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(identifier, nameof(identifier));

        // Init source blob
        var srcBlobClient = GetInstanceBlockBlob(identifier);

        // Init destination blob
        string destBlobName = GenerateDestinationBlobName(_destinationPath, identifier);
        var destBlobClient = _destClient.GetBlobClient(destBlobName);

        // Could not use StartCopyFromUriAsync from the SDK. There is a sourceUri auth issue and Azure team does not recommend using StartCopyFromUriAsync.
        // Open the source blob stream
        var blobOpenReadOptions = new BlobOpenReadOptions(allowModifications: false);
        using Stream stream = await srcBlobClient.OpenReadAsync(blobOpenReadOptions, cancellationToken);

        // Upload it to the destination
        var blobUploadOptions = new BlobUploadOptions { TransferOptions = _options.Upload };
        try
        {
            await destBlobClient.UploadAsync(stream, blobUploadOptions, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            CopyFailure?.Invoke(this, new CopyFailureEventArgs { Exception = ex, Identifier = identifier });
            await WriteErrorAsync(identifier, ex, cancellationToken);
            return false;
        }
    }

    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        // Move the stream back to the beginning
        _errorWriter.BaseStream.Seek(0, SeekOrigin.Begin);

        // Append the errors
        return _errorClient.AppendBlockAsync(_errorWriter.BaseStream, transactionalContentHash: null, conditions: null, progressHandler: null, cancellationToken);
    }

    private BlobClient GetInstanceBlockBlob(VersionedInstanceIdentifier versionedInstanceIdentifier)
    {
        string blobName = $"{versionedInstanceIdentifier.StudyInstanceUid}/{versionedInstanceIdentifier.SeriesInstanceUid}/{versionedInstanceIdentifier.SopInstanceUid}_{versionedInstanceIdentifier.Version}.dcm";
        return _sourceClient.GetBlobClient(blobName);
    }

    private static string GenerateDestinationBlobName(string destinationPath, VersionedInstanceIdentifier versionedInstanceIdentifier)
    {
        string destFileName = $"{versionedInstanceIdentifier.StudyInstanceUid}-{versionedInstanceIdentifier.SeriesInstanceUid}-{versionedInstanceIdentifier.SopInstanceUid}.dcm";

        return string.IsNullOrWhiteSpace(destinationPath) ? destFileName : $"{destinationPath}/{destFileName}";
    }

    private async ValueTask WriteErrorAsync(VersionedInstanceIdentifier identifier, Exception exception, CancellationToken cancellationToken = default)
    {
        await _errorWriter.WriteAsync($"{Clock.UtcNow},{DicomIdentifier.ForInstance(identifier)},{exception.Message}");
        await _errorWriter.FlushAsync();

        if (_errorWriter.BaseStream.Position >= BlockSize)
            await FlushAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await FlushAsync();
        _errorWriter.Dispose();
        GC.SuppressFinalize(this);
    }
}
