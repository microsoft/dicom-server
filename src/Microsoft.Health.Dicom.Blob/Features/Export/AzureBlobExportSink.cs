// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Buffers;
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

internal class AzureBlobExportSink : IExportSink
{
    public event EventHandler<CopyFailureEventArgs> CopyFailure;

    private readonly BlobContainerClient _sourceClient;
    private readonly BlobOperationOptions _options;
    private readonly BlobContainerClient _destClient;
    private readonly AsyncCache<AppendBlobClient> _errorAppendBlobcache;
    private readonly IMemoryOwner<byte> _buffer;
    private readonly Encoding _errorEncoding;
    private readonly string _destinationPath;
    private readonly string _errorLogBlobName;

    public AzureBlobExportSink(
        BlobServiceClient sourceClient,
        BlobContainerClient destClient,
        IMemoryOwner<byte> buffer,
        Encoding errorEncoding,
        IOptionsMonitor<BlobContainerConfiguration> sourceContainerOptions,
        IOptions<BlobOperationOptions> operationOptions,
        string destinationPath)
    {
        EnsureArg.IsNotNull(sourceClient, nameof(sourceClient));
        EnsureArg.IsNotNull(destClient, nameof(destClient));
        EnsureArg.IsNotNull(buffer, nameof(buffer));
        EnsureArg.IsNotNull(errorEncoding, nameof(errorEncoding));
        EnsureArg.IsNotNull(sourceContainerOptions, nameof(sourceContainerOptions));
        EnsureArg.IsNotNull(operationOptions?.Value, nameof(operationOptions));

        BlobContainerConfiguration containerConfiguration = sourceContainerOptions.Get(Constants.BlobContainerConfigurationName);
        _sourceClient = sourceClient.GetBlobContainerClient(containerConfiguration.ContainerName);
        _destClient = destClient;
        _buffer = buffer;
        _errorEncoding = errorEncoding;
        _options = operationOptions.Value;
        _destinationPath = destinationPath;
        _errorLogBlobName = $"error-{Guid.NewGuid()}.log";
        _errorAppendBlobcache = new AsyncCache<AppendBlobClient>(async (CancellationToken cancellationToken) =>
        {
            AppendBlobClient appendBlobClient = _sourceClient.GetAppendBlobClient(_errorLogBlobName);
            await appendBlobClient.CreateIfNotExistsAsync(options: null, cancellationToken);
            return appendBlobClient;
        });
    }

    public async Task<Uri> GetErrorHrefAsync(CancellationToken cancellationToken)
    {
        AppendBlobClient appendBlobClient = await _errorAppendBlobcache.GetAsync(forceRefresh: false, cancellationToken);
        return appendBlobClient.Uri;
    }

    public async Task CopyAsync(VersionedInstanceIdentifier instanceIdentifier, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));

        // Init source blob
        var srcBlobClient = GetInstanceBlockBlob(instanceIdentifier);

        // Init destination blob
        string destBlobName = GenerateDestinationBlobName(_destinationPath, instanceIdentifier);
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
        }
        catch (Exception ex)
        {
            OnCopyFailure(new CopyFailureEventArgs { Exception = ex, Identifier = instanceIdentifier });

        }
    }

    /// <summary>
    /// Appends operation logs to a destination file.
    /// Keep the content length below 4MB and max blocks to less than 50000
    /// </summary>
    /// <param name="content">Union logs to be blocks of upto 4MB</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task AppendErrorAsync(Stream content, CancellationToken cancellationToken)
    {
        AppendBlobClient appendBlobClient = await _errorAppendBlobcache.GetAsync(forceRefresh: false, cancellationToken);

        await ExecuteAsync(async () =>
        {
            await appendBlobClient.AppendBlockAsync(content, transactionalContentHash: null, conditions: null, progressHandler: null, cancellationToken);
        });
    }

    protected virtual void OnCopyFailure(CopyFailureEventArgs e)
        => CopyFailure?.Invoke(this, e);

    private BlobClient GetInstanceBlockBlob(VersionedInstanceIdentifier versionedInstanceIdentifier)
    {
        string blobName = $"{versionedInstanceIdentifier.StudyInstanceUid}/{versionedInstanceIdentifier.SeriesInstanceUid}/{versionedInstanceIdentifier.SopInstanceUid}_{versionedInstanceIdentifier.Version}.dcm";
        return _sourceClient.GetBlobClient(blobName);
    }

    private static string GenerateDestinationBlobName(string destinationPath, VersionedInstanceIdentifier versionedInstanceIdentifier)
    {
        return $"{destinationPath}/{versionedInstanceIdentifier.StudyInstanceUid}/{versionedInstanceIdentifier.SeriesInstanceUid}/{versionedInstanceIdentifier.SopInstanceUid}.dcm";
    }

    private ValueTask WriteErrorAsync(VersionedInstanceIdentifier identifier, Exception exception, CancellationToken cancellationToken = default)
    {
        byte[] bytes = _errorEncoding.GetBytes($"{Clock.UtcNow}, {DicomIdentifier.ForInstance(identifier)}, {exception.Message}");
    }

    private ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {

    }

    public ValueTask DisposeAsync()
        => FlushAsync();
}
