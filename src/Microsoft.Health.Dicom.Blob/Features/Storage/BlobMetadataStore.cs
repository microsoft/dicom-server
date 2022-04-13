// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
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
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store;
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
    private readonly UidAsInstanceName _uidAsInstanceName;
    private readonly WatermarkAsInstanceName _watermarkAsInstanceName;

    public BlobMetadataStore(
        BlobServiceClient client,
         UidAsInstanceName uidAsInstanceName,
        WatermarkAsInstanceName watermarkAsInstanceName,
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
        IOptions<JsonSerializerOptions> jsonSerializerOptions)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(jsonSerializerOptions?.Value, nameof(jsonSerializerOptions));
        EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
        EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));

        BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor
            .Get(Constants.MetadataContainerConfigurationName);

        _container = client.GetBlobContainerClient(containerConfiguration.ContainerName);
        _jsonSerializerOptions = jsonSerializerOptions.Value;
        _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        _uidAsInstanceName = EnsureArg.IsNotNull(uidAsInstanceName, nameof(uidAsInstanceName));
        _watermarkAsInstanceName = EnsureArg.IsNotNull(watermarkAsInstanceName, nameof(watermarkAsInstanceName));
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

        BlockBlobClient blob = GetInstanceBlockBlob(dicomDatasetWithoutBulkData.ToVersionedInstanceIdentifier(version));

        try
        {
            await using (Stream stream = _recyclableMemoryStreamManager.GetStream(StoreInstanceMetadataStreamTagName))
            await using (Utf8JsonWriter utf8Writer = new Utf8JsonWriter(stream))
            {
                // TODO: Use SerializeAsync in .NET 6
                JsonSerializer.Serialize(utf8Writer, dicomDatasetWithoutBulkData, _jsonSerializerOptions);
                await utf8Writer.FlushAsync(cancellationToken);
                stream.Seek(0, SeekOrigin.Begin);
                await blob.UploadAsync(
                    stream,
                    new BlobHttpHeaders { ContentType = KnownContentTypes.ApplicationJson },
                    metadata: null,
                    conditions: null,
                    accessTier: null,
                    progressHandler: null,
                    cancellationToken);
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
        BlockBlobClient blob = GetInstanceBlockBlob(versionedInstanceIdentifier);

        await ExecuteAsync(t => blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, conditions: null, t), cancellationToken);
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

    private BlockBlobClient GetInstanceBlockBlob(VersionedInstanceIdentifier versionedInstanceIdentifier, bool duplicated = false)
    {
        IInstanceNameBuilder nameBuilder = duplicated ? _watermarkAsInstanceName : _uidAsInstanceName;
        string blobName = nameBuilder.GetInstanceMetadataFileName(versionedInstanceIdentifier);
        return _container.GetBlockBlobClient(blobName);
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

    public async Task DuplicateInstanceMetadataAsync(VersionedInstanceIdentifier identifier, CancellationToken cancellationToken)
    {
        var blob = GetInstanceBlockBlob(identifier, duplicated: false);
        var duplicatedBlob = GetInstanceBlockBlob(identifier, duplicated: true);
        if (!await duplicatedBlob.ExistsAsync(cancellationToken))
        {
            var operation = await duplicatedBlob.StartCopyFromUriAsync(blob.Uri, options: null, cancellationToken);
            await operation.WaitForCompletionAsync(cancellationToken);
        }
    }
}
