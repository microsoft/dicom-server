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
    private readonly bool _enableDualWrite;
    private readonly bool _supportNewBlobFormatForNewService;
    private readonly DicomFileNameWithUID _fileNameWithUID;
    private readonly DicomFileNameWithPrefix _nameWithPrefix;

    public BlobMetadataStore(
        BlobServiceClient client,
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        DicomFileNameWithUID fileNameWithUID,
        DicomFileNameWithPrefix nameWithPrefix,
        IOptions<FeatureConfiguration> featureConfiguration,
        IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
        IOptions<JsonSerializerOptions> jsonSerializerOptions)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(jsonSerializerOptions?.Value, nameof(jsonSerializerOptions));
        EnsureArg.IsNotNull(fileNameWithUID, nameof(fileNameWithUID));
        EnsureArg.IsNotNull(nameWithPrefix, nameof(nameWithPrefix));
        EnsureArg.IsNotNull(featureConfiguration, nameof(featureConfiguration));
        EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
        EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));

        BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor
            .Get(Constants.MetadataContainerConfigurationName);

        _container = client.GetBlobContainerClient(containerConfiguration.ContainerName);
        _jsonSerializerOptions = jsonSerializerOptions.Value;
        _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        _fileNameWithUID = fileNameWithUID;
        _nameWithPrefix = nameWithPrefix;
        _enableDualWrite = featureConfiguration.Value.EnableDualWrite;
        _supportNewBlobFormatForNewService = featureConfiguration.Value.SupportNewBlobFormatForNewService;
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

        BlockBlobClient[] blobs = GetInstanceBlockBlobs(dicomDatasetWithoutBulkData.ToVersionedInstanceIdentifier(version));

        try
        {
            await using (Stream stream = _recyclableMemoryStreamManager.GetStream(StoreInstanceMetadataStreamTagName))
            await using (Utf8JsonWriter utf8Writer = new Utf8JsonWriter(stream))
            {
                // TODO: Use SerializeAsync in .NET 6
                JsonSerializer.Serialize(utf8Writer, dicomDatasetWithoutBulkData, _jsonSerializerOptions);
                await utf8Writer.FlushAsync(cancellationToken);
                stream.Seek(0, SeekOrigin.Begin);

                await Task.WhenAll(blobs.Select(blob => blob.UploadAsync(
                    stream,
                    new BlobHttpHeaders { ContentType = KnownContentTypes.ApplicationJson },
                    metadata: null,
                    conditions: null,
                    accessTier: null,
                    progressHandler: null,
                    cancellationToken)));
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
        BlockBlobClient[] blobs = GetInstanceBlockBlobs(versionedInstanceIdentifier);

        await Task.WhenAll(blobs.Select(blob => ExecuteAsync(t => blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, conditions: null, t), cancellationToken)));
    }

    /// <inheritdoc />
    public Task<DicomDataset> GetInstanceMetadataAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));
        BlockBlobClient cloudBlockBlob = GetInstanceBlockBlob(versionedInstanceIdentifier);

        return ExecuteAsync(async t =>
        {
            await using (Stream stream = _recyclableMemoryStreamManager.GetStream(GetInstanceMetadataStreamTagName))
            {
                await cloudBlockBlob.DownloadToAsync(stream, cancellationToken);

                stream.Seek(0, SeekOrigin.Begin);

                return await JsonSerializer.DeserializeAsync<DicomDataset>(stream, _jsonSerializerOptions, t);
            }
        }, cancellationToken);
    }

    // TODO: This should removed once we migrate everything and the global flag is turned on
    private BlockBlobClient GetInstanceBlockBlob(VersionedInstanceIdentifier versionedInstanceIdentifier)
    {
        string blobName;
        if (_supportNewBlobFormatForNewService)
        {
            blobName = _nameWithPrefix.GetMetadataFileName(versionedInstanceIdentifier);
        }
        else
        {
            blobName = _fileNameWithUID.GetMetadataFileName(versionedInstanceIdentifier);
        }

        return _container.GetBlockBlobClient(blobName);
    }

    private BlockBlobClient[] GetInstanceBlockBlobs(VersionedInstanceIdentifier versionedInstanceIdentifier)
    {
        var clients = new List<BlockBlobClient>();

        string blobName;

        if (_supportNewBlobFormatForNewService)
        {
            blobName = _nameWithPrefix.GetMetadataFileName(versionedInstanceIdentifier);
            clients.Add(_container.GetBlockBlobClient(blobName));
        }
        else if (_enableDualWrite)
        {
            blobName = _fileNameWithUID.GetMetadataFileName(versionedInstanceIdentifier);
            clients.Add(_container.GetBlockBlobClient(blobName));

            blobName = _nameWithPrefix.GetMetadataFileName(versionedInstanceIdentifier);
            clients.Add(_container.GetBlockBlobClient(blobName));
        }
        else
        {
            blobName = _fileNameWithUID.GetMetadataFileName(versionedInstanceIdentifier);
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
