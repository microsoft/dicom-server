// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.IO;
using FellowOakDicom.IO.Writer;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Models.Update;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core.Features.Update;
public class UpdateInstanceService : IUpdateInstanceService
{
    private readonly IFileStore _fileStore;
    private readonly IMetadataStore _metadataStore;
    private readonly ILogger<UpdateInstanceService> _logger;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private const int LargeObjectsizeInBytes = 1024 * 1024;
    private const int StageBlockSizeInBytes = 4 * 1024 * 1024;
    private readonly IDicomOperationsClient _client;
    private readonly IGuidFactory _guidFactory;

    public UpdateInstanceService(
        IFileStore fileStore,
        IMetadataStore metadataStore,
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        ILogger<UpdateInstanceService> logger,
        IDicomOperationsClient client,
        IGuidFactory guidFactory)
    {
        _fileStore = EnsureArg.IsNotNull(fileStore, nameof(fileStore));
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        _recyclableMemoryStreamManager = EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));
        _client = EnsureArg.IsNotNull(client, nameof(client));
        _guidFactory = EnsureArg.IsNotNull(guidFactory, nameof(guidFactory));
    }

    public async Task QueueUpdateOperationAsync(
        UpdateSpecification updateSpecification,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(updateSpecification, nameof(updateSpecification));
        EnsureArg.IsNotNull(updateSpecification.ChangeDataset, nameof(updateSpecification.ChangeDataset));

        var operationId = _guidFactory.Create();
        var partitionKey = 1;

        await _client.StartUpdateOperationAsync(operationId, updateSpecification, partitionKey, cancellationToken);
    }

    public async Task UpdateInstanceBlobAsync(InstanceMetadata instanceMetadata, DicomDataset datasetToUpdate, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(datasetToUpdate, nameof(datasetToUpdate));
        EnsureArg.IsNotNull(instanceMetadata, nameof(instanceMetadata));
        EnsureArg.IsTrue(instanceMetadata.InstanceProperties.NewVersion.HasValue, nameof(instanceMetadata.InstanceProperties.NewVersion.HasValue));

        Task updateInstanceFileTask = UpdateInstanceFileAsync(instanceMetadata, datasetToUpdate, cancellationToken);
        Task updateInstanceMetadataTask = UpdateInstanceMetadataAsync(instanceMetadata, datasetToUpdate, cancellationToken);
        await Task.WhenAll(updateInstanceFileTask, updateInstanceMetadataTask);
    }

    private async Task UpdateInstanceFileAsync(InstanceMetadata instanceMetadata, DicomDataset datasetToUpdate, CancellationToken cancellationToken)
    {
        long originFileIdentifier = instanceMetadata.VersionedInstanceIdentifier.Version;
        long newFileIdentifier = instanceMetadata.InstanceProperties.NewVersion.Value;

        KeyValuePair<string, long> block = default;

        // If the file is already updated, then we can copy the file and just update the first block
        if (instanceMetadata.InstanceProperties.OriginalVersion.HasValue)
        {
            await _fileStore.CopyFileAsync(originFileIdentifier, newFileIdentifier, cancellationToken);
        }
        else
        {
            Stream stream = await _fileStore.GetStreamingFileAsync(originFileIdentifier, cancellationToken);
            DicomFile dcmFile = await DicomFile.OpenAsync(stream, FileReadOption.ReadLargeOnDemand);

            long firstBlockLength = stream.Length;
            // If the file is greater than 1MB try to upload in different blocks
            if (stream.Length > LargeObjectsizeInBytes)
            {
                _ = GetDatasetSize(dcmFile.Dataset, out DicomItem largeDicomItem);

                if (largeDicomItem != null)
                    RemoveItemsFromDataset(dcmFile.Dataset, largeDicomItem);

                firstBlockLength = await GetFirstBlockLengthAsync(dcmFile);
            }

            IDictionary<string, long> blockLengths = GetBlockLengths(stream.Length, firstBlockLength);

            await _fileStore.StoreFileInBlocksAsync(newFileIdentifier, stream, blockLengths, cancellationToken);

            block = blockLengths.First();
        }

        await UpdateDatasetInFileAsync(newFileIdentifier, datasetToUpdate, block, cancellationToken);
    }

    private async Task UpdateInstanceMetadataAsync(InstanceMetadata instanceMetadata, DicomDataset datasetToUpdate, CancellationToken cancellationToken)
    {
        long originFileIdentifier = instanceMetadata.VersionedInstanceIdentifier.Version;
        long newFileIdentifier = instanceMetadata.InstanceProperties.NewVersion.Value;

        DicomDataset metadata = await _metadataStore.GetInstanceMetadataAsync(originFileIdentifier, cancellationToken);
        metadata.AddOrUpdate(datasetToUpdate);

        await _metadataStore.StoreInstanceMetadataAsync(metadata, newFileIdentifier, cancellationToken);
    }

    private async Task UpdateDatasetInFileAsync(long newFileIdentifier, DicomDataset datasetToUpdate, KeyValuePair<string, long> block = default, CancellationToken cancellationToken = default)
    {
        // If the block is not provided, then we need to get the first block to update the dataset
        if (block.Key is null)
        {
            block = await _fileStore.GetFirstBlockPropertyAsync(newFileIdentifier, cancellationToken);
        }

        Stream stream = await _fileStore.GetFileInRangeAsync(newFileIdentifier, new FrameRange(0, block.Value), cancellationToken);

        DicomFile dicomFile = await DicomFile.OpenAsync(stream);
        dicomFile.Dataset.AddOrUpdate(datasetToUpdate);

        MemoryStream resultStream = _recyclableMemoryStreamManager.GetStream();
        await dicomFile.SaveAsync(resultStream);

        await _fileStore.UpdateFileBlockAsync(newFileIdentifier, block.Key, stream, cancellationToken);
    }

    private static IDictionary<string, long> GetBlockLengths(long streamLength, long initialBlockLength)
    {
        var blockLengths = new Dictionary<string, long>();
        long fileSizeWithoutFirstBlock = streamLength - initialBlockLength;
        int numStagesWithoutFirstBlock = (int)Math.Ceiling((double)fileSizeWithoutFirstBlock / StageBlockSizeInBytes) + 1;

        long bytesRead = 0;
        for (int i = 0; i < numStagesWithoutFirstBlock; i++)
        {
            long blockSize = i == 0 ? initialBlockLength : Math.Min(StageBlockSizeInBytes, streamLength - bytesRead);
            string blockId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            blockLengths.Add(blockId, blockSize);
            bytesRead += blockSize;
        }

        return blockLengths;
    }

    private static long GetDatasetSize(DicomDataset dataset, out DicomItem largeDicomItem)
    {
        long totalSize = 0;
        largeDicomItem = null;

        foreach (var item in dataset)
        {
            if (item is DicomElement)
            {
                var dicomElement = (DicomElement)item;
                totalSize += dicomElement.Buffer.Size;

                if (dicomElement.Buffer.Size > LargeObjectsizeInBytes)
                {
                    largeDicomItem = item;
                    break;
                }
            }
            else if (item is DicomFragmentSequence)
            {
                var fragmentSequence = item as DicomFragmentSequence;
                long fragmentSize = 0;
                foreach (var fragmentItem in fragmentSequence)
                {
                    fragmentSize += fragmentItem.Size;
                }

                totalSize += fragmentSize;

                if (fragmentSize > LargeObjectsizeInBytes)
                {
                    largeDicomItem = item;
                    break;
                }
            }
            else if (item is DicomSequence)
            {
                var sequence = item as DicomSequence;
                long sequenceSize = 0;
                foreach (var sequenceItem in sequence)
                {
                    sequenceSize += GetDatasetSize(sequenceItem, out largeDicomItem);
                    totalSize += sequenceSize;
                }

                if (sequenceSize > LargeObjectsizeInBytes)
                {
                    largeDicomItem = item;
                    break;
                }
            }
        }

        return totalSize;
    }

    private async Task<long> GetFirstBlockLengthAsync(DicomFile dcmFile)
    {
        DicomDataset dataset = dcmFile.Dataset;

        var writeOptions = new DicomWriteOptions();
        MemoryStream resultStream = _recyclableMemoryStreamManager.GetStream();

        var target = new StreamByteTarget(resultStream);

        var writer = new DicomFileWriter(writeOptions);
        await writer.WriteAsync(target, dcmFile.FileMetaInfo, dataset);
        return resultStream.Length;
    }

    private static void RemoveItemsFromDataset(DicomDataset dataset, DicomItem largeDicomItem)
    {
        bool toRemove = false;
        var dicomTags = new List<DicomTag>();

        foreach (var item in dataset)
        {
            if (item.Tag == largeDicomItem.Tag)
            {
                toRemove = true;
            }

            if (toRemove)
            {
                dicomTags.Add(item.Tag);
            }
        }

        dataset.Remove(dicomTags.ToArray());
    }
}
