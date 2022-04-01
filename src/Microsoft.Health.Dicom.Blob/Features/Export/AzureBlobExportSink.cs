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
using Microsoft.Health.Dicom.Features.Common;

namespace Microsoft.Health.Dicom.Blob.Features.Export;

internal sealed class AzureBlobExportSink : IExportSink
{
    public event EventHandler<CopyFailureEventArgs> CopyFailure;

    private readonly BlobContainerClient _sourceClient;
    private readonly BlobOperationOptions _options;
    private readonly BlobContainerClient _destClient;
    private readonly AsyncCache<AppendBlobClient> _errorClientCache;
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
        _errorClientCache = new AsyncCache<AppendBlobClient>(async (CancellationToken cancellationToken) =>
        {
            AppendBlobClient client = _sourceClient.GetAppendBlobClient(errorBlobName);
            await client.CreateAsync(options: null, cancellationToken);
            return client;
        });
        _errorWriter = new StreamWriter(new MemoryStream(BlockSize), errorEncoding);
        _destinationPath = destinationPath;
        _options = operationOptions.Value;
    }

    public async Task<Uri> GetErrorHrefAsync(CancellationToken cancellationToken)
    {
        AppendBlobClient blobClient = await _errorClientCache.GetAsync(forceRefresh: false, cancellationToken);
        return blobClient.Uri;
    }

    public async Task<bool> CopyAsync(SourceElement element, CancellationToken cancellationToken)
    {
        if (element.Failure != null)
        {
            await WriteErrorAsync(element.Failure.Identifier, element.Failure.Exception, cancellationToken);
            return false;
        }
        else
        {
            // Init source blob
            var srcBlobClient = GetInstanceBlockBlob(element.Identifier);

            // Init destination blob
            string destBlobName = GenerateDestinationBlobName(_destinationPath, element.Identifier);
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
                CopyFailure?.Invoke(this, new CopyFailureEventArgs(element.Identifier, ex));
                await WriteErrorAsync(DicomIdentifier.ForInstance(element.Identifier), ex, cancellationToken);
                return false;
            }
        }
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        // Move the stream back to the beginning
        _errorWriter.BaseStream.Seek(0, SeekOrigin.Begin);

        // Append the errors
        AppendBlobClient errorblobClient = await _errorClientCache.GetAsync(forceRefresh: false, cancellationToken);
        await errorblobClient.AppendBlockAsync(_errorWriter.BaseStream, transactionalContentHash: null, conditions: null, progressHandler: null, cancellationToken);

        // reset the stream
        _errorWriter.BaseStream.SetLength(0);
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

    private async ValueTask WriteErrorAsync(DicomIdentifier identifier, Exception exception, CancellationToken cancellationToken = default)
    {
        await _errorWriter.WriteAsync($"{Clock.UtcNow},{identifier},{exception}");
        await _errorWriter.FlushAsync();

        if (_errorWriter.BaseStream.Position >= BlockSize)
            await FlushAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await FlushAsync();
        _errorWriter.Dispose();
        _errorClientCache.Dispose();
        GC.SuppressFinalize(this);
    }
}
