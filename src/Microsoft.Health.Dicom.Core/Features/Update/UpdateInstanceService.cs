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
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.IO;
using DicomFileExtensions = Microsoft.Health.Dicom.Core.Features.Retrieve.DicomFileExtensions;

namespace Microsoft.Health.Dicom.Core.Features.Update;

public class UpdateInstanceService : IUpdateInstanceService
{
    private readonly IFileStore _fileStore;
    private readonly IMetadataStore _metadataStore;
    private readonly ILogger<UpdateInstanceService> _logger;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private const int LargeObjectsizeInBytes = 1024 * 1024;
    private const int StageBlockSizeInBytes = 4 * 1024 * 1024;

    public UpdateInstanceService(
        IFileStore fileStore,
        IMetadataStore metadataStore,
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        ILogger<UpdateInstanceService> logger)
    {
        _fileStore = EnsureArg.IsNotNull(fileStore, nameof(fileStore));
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        _recyclableMemoryStreamManager = EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));
    }

    /// <inheritdoc />
    public async Task UpdateInstanceBlobAsync(InstanceFileIdentifier instanceFileIdentifier, DicomDataset datasetToUpdate, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(datasetToUpdate, nameof(datasetToUpdate));
        EnsureArg.IsNotNull(instanceFileIdentifier, nameof(instanceFileIdentifier));
        EnsureArg.IsTrue(instanceFileIdentifier.NewVersion.HasValue, nameof(instanceFileIdentifier.NewVersion.HasValue));

        Task updateInstanceFileTask = UpdateInstanceFileAsync(instanceFileIdentifier, datasetToUpdate, cancellationToken);
        Task updateInstanceMetadataTask = UpdateInstanceMetadataAsync(instanceFileIdentifier, datasetToUpdate, cancellationToken);
        await Task.WhenAll(updateInstanceFileTask, updateInstanceMetadataTask);
    }

    /// <inheritdoc />
    public async Task DeleteInstanceBlobAsync(long fileIdentifier, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Begin deleting instance blob {FileIdentifier}.", fileIdentifier);

        Task fileTask = _fileStore.DeleteFileIfExistsAsync(fileIdentifier, cancellationToken);
        Task metadataTask = _metadataStore.DeleteInstanceMetadataIfExistsAsync(fileIdentifier, cancellationToken);

        await Task.WhenAll(fileTask, metadataTask);

        _logger.LogInformation("Deleting instance blob {FileIdentifier} completed successfully.", fileIdentifier);
    }

    private async Task UpdateInstanceFileAsync(InstanceFileIdentifier instanceFileIdentifier, DicomDataset datasetToUpdate, CancellationToken cancellationToken)
    {
        long originFileIdentifier = instanceFileIdentifier.Version;
        long newFileIdentifier = instanceFileIdentifier.NewVersion.Value;

        KeyValuePair<string, long> block = default;

        // If the file is already updated, then we can copy the file and just update the first block
        if (instanceFileIdentifier.OriginalVersion.HasValue)
        {
            _logger.LogInformation("Begin copying instance file {OrignalFileIdentifier} - {NewFileIdentifier}", originFileIdentifier, newFileIdentifier);
            await _fileStore.CopyFileAsync(originFileIdentifier, newFileIdentifier, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Begin downloading original file {OrignalFileIdentifier} - {NewFileIdentifier}", originFileIdentifier, newFileIdentifier);

            using Stream stream = await _fileStore.GetFileAsync(originFileIdentifier, cancellationToken);
            DicomFile dcmFile = await DicomFile.OpenAsync(stream, FileReadOption.ReadLargeOnDemand);

            long firstBlockLength = stream.Length;

            // If the file is greater than 1MB try to upload in different blocks.
            // Since we are supporting updating only Patient demographic and identification module,
            // we assume patient attributes are at the very beginning of the dataset.
            // If in future, we support updating other attributes like private tags, which could appear at the end
            // We need to read the whole file instead of first block.
            if (stream.Length > LargeObjectsizeInBytes)
            {
                _logger.LogInformation("Found bigger DICOM item, splitting the file into multiple blocks. {OrignalFileIdentifier} - {NewFileIdentifier}", originFileIdentifier, newFileIdentifier);

                if (dcmFile.Dataset.TryGetLargeDicomItem(LargeObjectsizeInBytes, StageBlockSizeInBytes, out DicomItem largeDicomItem))
                {
                    RemoveItemsAfter(dcmFile.Dataset, largeDicomItem.Tag);
                }

                firstBlockLength = await DicomFileExtensions.GetByteLengthAsync(dcmFile, _recyclableMemoryStreamManager);
            }

            IDictionary<string, long> blockLengths = GetBlockLengths(stream.Length, firstBlockLength);

            _logger.LogInformation("Begin uploading instance file in blocks {OrignalFileIdentifier} - {NewFileIdentifier}", originFileIdentifier, newFileIdentifier);

            stream.Seek(0, SeekOrigin.Begin);

            await _fileStore.StoreFileInBlocksAsync(newFileIdentifier, stream, blockLengths, cancellationToken);

            block = blockLengths.First();

            _logger.LogInformation("Uploading instance file in blocks {FileIdentifier} completed successfully", newFileIdentifier);
        }

        await UpdateDatasetInFileAsync(newFileIdentifier, datasetToUpdate, block, cancellationToken);
    }

    private async Task UpdateInstanceMetadataAsync(InstanceFileIdentifier instanceFileIdentifier, DicomDataset datasetToUpdate, CancellationToken cancellationToken)
    {
        long originFileIdentifier = instanceFileIdentifier.Version;
        long newFileIdentifier = instanceFileIdentifier.NewVersion.Value;

        _logger.LogInformation("Begin downloading original file metdata {OrignalFileIdentifier} - {NewFileIdentifier}", originFileIdentifier, newFileIdentifier);

        DicomDataset metadata = await _metadataStore.GetInstanceMetadataAsync(originFileIdentifier, cancellationToken);
        metadata.AddOrUpdate(datasetToUpdate);

        await _metadataStore.StoreInstanceMetadataAsync(metadata, newFileIdentifier, cancellationToken);

        _logger.LogInformation("Updating metadata file {OrignalFileIdentifier} - {NewFileIdentifier} completed successfully", originFileIdentifier, newFileIdentifier);
    }

    private async Task UpdateDatasetInFileAsync(long newFileIdentifier, DicomDataset datasetToUpdate, KeyValuePair<string, long> block = default, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Begin updating new file {NewFileIdentifier}", newFileIdentifier);

        // If the block is not provided, then we need to get the first block to update the dataset
        if (block.Key is null)
        {
            block = await _fileStore.GetFirstBlockPropertyAsync(newFileIdentifier, cancellationToken);
        }

        BinaryData data = await _fileStore.GetFileInRangeAsync(newFileIdentifier, new FrameRange(0, block.Value), cancellationToken);

        using MemoryStream stream = _recyclableMemoryStreamManager.GetStream(data);
        DicomFile dicomFile = await DicomFile.OpenAsync(stream);
        dicomFile.Dataset.AddOrUpdate(datasetToUpdate);

        using MemoryStream resultStream = _recyclableMemoryStreamManager.GetStream();
        await dicomFile.SaveAsync(resultStream);

        await _fileStore.UpdateFileBlockAsync(newFileIdentifier, block.Key, resultStream, cancellationToken);

        _logger.LogInformation("Updating new file {NewFileIdentifier} completed successfully", newFileIdentifier);
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

    // Removes all items in the list after the specified item tag
    private static void RemoveItemsAfter(DicomDataset dataset, DicomTag tag)
    {
        bool toRemove = false;
        var dicomTags = new List<DicomTag>();

        foreach (var item in dataset)
        {
            if (item.Tag == tag)
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
