// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Features.Common;

namespace Microsoft.Health.Dicom.Blob.Features.Storage;

/// <summary>
/// Initialize this class once for each export operation
/// </summary>
public sealed class BlobCopyStore : IBlobCopyStore
{
    private readonly BlobContainerClient _sourceContainer;
    private readonly BlobOperationOptions _options;
    private readonly BlobContainerClient _destBlobContainerClient;
    private readonly AsyncCache<AppendBlobClient> _errorAppendBlobcache;
    private readonly string _destinationPath;
    private readonly string _errorLogBlobName;

    public BlobCopyStore(
        BlobServiceClient client,
        IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
        IOptions<BlobOperationOptions> options,
        BlobContainerClient destBlobContainerClient,
        string destinationPath)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
        EnsureArg.IsNotNull(options?.Value, nameof(options));
        EnsureArg.IsNotNull(destBlobContainerClient, nameof(destBlobContainerClient));

        BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor
            .Get(Constants.BlobContainerConfigurationName);

        _sourceContainer = client.GetBlobContainerClient(containerConfiguration.ContainerName);
        _options = options.Value;
        _destBlobContainerClient = destBlobContainerClient;
        _destinationPath = destinationPath;
        _errorLogBlobName = $"error-{Guid.NewGuid().ToString()}.log";
        _errorAppendBlobcache = new AsyncCache<AppendBlobClient>(async (CancellationToken cancellationToken) =>
            {
                AppendBlobClient appendBlobClient = _sourceContainer.GetAppendBlobClient(_errorLogBlobName);
                await appendBlobClient.CreateIfNotExistsAsync(options: null, cancellationToken);
                return appendBlobClient;
            });

    }

    public async Task<Uri> GetErrorHrefAsync(CancellationToken cancellationToken)
    {
        AppendBlobClient appendBlobClient = await _errorAppendBlobcache.GetAsync(forceRefresh: false, cancellationToken);
        return appendBlobClient.Uri;
    }

    public async Task CopyFileAsync(VersionedInstanceIdentifier instanceIdentifier, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));

        // Init source blob
        var srcBlobClient = GetInstanceBlockBlob(instanceIdentifier);

        // Init destination blob
        string destBlobName = GenerateDestinationBlobName(_destinationPath, instanceIdentifier);
        var destBlobClient = _destBlobContainerClient.GetBlobClient(destBlobName);

        // Could not use StartCopyFromUriAsync from the SDK. There is a sourceUri auth issue and Azure team does not recommend using StartCopyFromUriAsync.
        // Open the source blob stream
        var blobOpenReadOptions = new BlobOpenReadOptions(allowModifications: false);
        using Stream stream = await srcBlobClient.OpenReadAsync(blobOpenReadOptions, cancellationToken);

        // Upload it to the destination
        var blobUploadOptions = new BlobUploadOptions { TransferOptions = _options.Upload };
        await ExecuteAsync(async () =>
        {
            await destBlobClient.UploadAsync(
                stream,
                blobUploadOptions,
                cancellationToken);
        });
    }

    /// <summary>
    /// Appends operation logs to a destination file.
    /// Keep the content length below 4MB and max blocks to less than 50000
    /// </summary>
    /// <param name="content">Union logs to be blocks of upto 4MB</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task AppendErrorLogAsync(Stream content, CancellationToken cancellationToken)
    {
        AppendBlobClient appendBlobClient = await _errorAppendBlobcache.GetAsync(forceRefresh: false, cancellationToken);

        await ExecuteAsync(async () =>
        {
            await appendBlobClient.AppendBlockAsync(content, transactionalContentHash: null, conditions: null, progressHandler: null, cancellationToken);
        });
    }

    public void Dispose()
    {
        _errorAppendBlobcache.Dispose();
        GC.SuppressFinalize(this);
    }

    private BlobClient GetInstanceBlockBlob(VersionedInstanceIdentifier versionedInstanceIdentifier)
    {
        string blobName = $"{versionedInstanceIdentifier.StudyInstanceUid}/{versionedInstanceIdentifier.SeriesInstanceUid}/{versionedInstanceIdentifier.SopInstanceUid}_{versionedInstanceIdentifier.Version}.dcm";
        return _sourceContainer.GetBlobClient(blobName);
    }

    private static string GenerateDestinationBlobName(string destinationPath, VersionedInstanceIdentifier versionedInstanceIdentifier)
    {
        return $"{destinationPath}/{versionedInstanceIdentifier.StudyInstanceUid}/{versionedInstanceIdentifier.SeriesInstanceUid}/{versionedInstanceIdentifier.SopInstanceUid}.dcm";
    }

    private static async Task ExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            throw new DataStoreException(ex);
        }
    }
}
