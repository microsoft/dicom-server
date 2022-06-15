// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Blob.Features.Storage;

/// <summary>
/// Provides functionality for managing the DICOM instance metadata.
/// </summary>
public class BlobMetadataStore : IMetadataStore
{
    private const string GetInstanceMetadataStreamTagName = nameof(BlobMetadataStore) + "." + nameof(GetInstanceMetadataAsync);
    private const string StoreInstanceMetadataStreamTagName = nameof(BlobMetadataStore) + "." + nameof(StoreInstanceMetadataAsync);

    private readonly BlobContainerClient _container;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly BlobMigrationFormatType _blobMigrationFormatType;
    private readonly DicomFileNameWithUid _nameWithUid;
    private readonly DicomFileNameWithPrefix _nameWithPrefix;

    public BlobMetadataStore(
        BlobServiceClient client,
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        DicomFileNameWithUid fileNameWithUid,
        DicomFileNameWithPrefix nameWithPrefix,
        IOptions<BlobMigrationConfiguration> blobMigrationFormatConfiguration,
        IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
        IOptions<JsonSerializerOptions> jsonSerializerOptions)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(jsonSerializerOptions?.Value, nameof(jsonSerializerOptions));
        EnsureArg.IsNotNull(fileNameWithUid, nameof(fileNameWithUid));
        EnsureArg.IsNotNull(nameWithPrefix, nameof(nameWithPrefix));
        EnsureArg.IsNotNull(blobMigrationFormatConfiguration, nameof(blobMigrationFormatConfiguration));
        EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
        EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));

        BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor
            .Get(Constants.MetadataContainerConfigurationName);

        _container = client.GetBlobContainerClient(containerConfiguration.ContainerName);
        _jsonSerializerOptions = jsonSerializerOptions.Value;
        _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        _nameWithUid = fileNameWithUid;
        _nameWithPrefix = nameWithPrefix;
        _blobMigrationFormatType = blobMigrationFormatConfiguration.Value.FormatType;
    }

    /// <inheritdoc />
    public async Task StoreInstanceMetadataAsync(
        DicomDataset dicomDataset,
        long version,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

        // Creates a copy of the dataset with bulk data removed.
        DicomDataset dicomDatasetWithoutBulkData = dicomDataset.CopyWithoutBulkDataItems();

        BlockBlobClient[] blobClients = GetInstanceBlockBlobClients(dicomDatasetWithoutBulkData.ToVersionedInstanceIdentifier(version));

        var taskResponse = new List<Task<Response<BlobContentInfo>>>();

        try
        {
            await using (Stream stream = _recyclableMemoryStreamManager.GetStream(StoreInstanceMetadataStreamTagName))
            await using (Utf8JsonWriter utf8Writer = new Utf8JsonWriter(stream))
            {
                // TODO: Use SerializeAsync in .NET 6
                JsonSerializer.Serialize(utf8Writer, dicomDatasetWithoutBulkData, _jsonSerializerOptions);
                await utf8Writer.FlushAsync(cancellationToken);

                foreach (var blob in blobClients)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    taskResponse.Add(blob.UploadAsync(
                        stream,
                        new BlobHttpHeaders { ContentType = KnownContentTypes.ApplicationJson },
                        metadata: null,
                        conditions: null,
                        accessTier: null,
                        progressHandler: null,
                        cancellationToken));
                }

                await Task.WhenAll(taskResponse);
            }
        }
        catch (Exception ex)
        {
            throw new DataStoreException(ex);
        }
    }

    /// <inheritdoc />
    public async Task DeleteInstanceMetadataIfExistsAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));
        BlockBlobClient[] blobClients = GetInstanceBlockBlobClients(versionedInstanceIdentifier);

        await Task.WhenAll(blobClients.Select(blob => ExecuteAsync(t => blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, conditions: null, t), cancellationToken)));
    }

    /// <inheritdoc />
    public Task<DicomDataset> GetInstanceMetadataAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));
        BlockBlobClient blobClient = GetInstanceBlockBlobClient(versionedInstanceIdentifier, _blobMigrationFormatType);

        return ExecuteAsync(async t =>
        {
            await using (Stream stream = _recyclableMemoryStreamManager.GetStream(GetInstanceMetadataStreamTagName))
            {
                await blobClient.DownloadToAsync(stream, cancellationToken);

                stream.Seek(0, SeekOrigin.Begin);

                return await JsonSerializer.DeserializeAsync<DicomDataset>(stream, _jsonSerializerOptions, t);
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task CopyInstanceMetadataAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

        var blobClient = GetInstanceBlockBlobClient(versionedInstanceIdentifier, BlobMigrationFormatType.Old);
        var copyBlobClient = GetInstanceBlockBlobClient(versionedInstanceIdentifier, BlobMigrationFormatType.New);

        if (!await copyBlobClient.ExistsAsync(cancellationToken))
        {
            var operation = await copyBlobClient.StartCopyFromUriAsync(blobClient.Uri, options: null, cancellationToken);
            await operation.WaitForCompletionAsync(cancellationToken);
        }
    }

    private BlockBlobClient GetInstanceBlockBlobClient(VersionedInstanceIdentifier versionedInstanceIdentifier, BlobMigrationFormatType formatType)
    {
        string blobName;
        if (formatType == BlobMigrationFormatType.New)
        {
            blobName = _nameWithPrefix.GetMetadataFileName(versionedInstanceIdentifier);
        }
        else
        {
            blobName = _nameWithUid.GetMetadataFileName(versionedInstanceIdentifier);
        }

        return _container.GetBlockBlobClient(blobName);
    }

    // TODO: This should removed once we migrate everything and the global flag is turned on
    private BlockBlobClient[] GetInstanceBlockBlobClients(VersionedInstanceIdentifier versionedInstanceIdentifier)
    {
        var clients = new List<BlockBlobClient>(2);

        string blobName;

        if (_blobMigrationFormatType == BlobMigrationFormatType.New)
        {
            blobName = _nameWithPrefix.GetMetadataFileName(versionedInstanceIdentifier);
            clients.Add(_container.GetBlockBlobClient(blobName));
        }
        else if (_blobMigrationFormatType == BlobMigrationFormatType.Dual)
        {
            blobName = _nameWithUid.GetMetadataFileName(versionedInstanceIdentifier);
            clients.Add(_container.GetBlockBlobClient(blobName));

            blobName = _nameWithPrefix.GetMetadataFileName(versionedInstanceIdentifier);
            clients.Add(_container.GetBlockBlobClient(blobName));
        }
        else
        {
            blobName = _nameWithUid.GetMetadataFileName(versionedInstanceIdentifier);
            clients.Add(_container.GetBlockBlobClient(blobName));
        }

        return clients.ToArray();
    }

    private static async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        try
        {
            return await action(cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobNotFound)
        {
            throw new ItemNotFoundException(ex);
        }
        catch (Exception ex)
        {
            throw new DataStoreException(ex);
        }
    }
}
