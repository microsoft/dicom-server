// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core.Features.Update;

public class UpdateInstanceService : IUpdateInstanceService
{
    private readonly IFileStore _fileStore;
    private readonly IMetadataStore _metadataStore;
    private readonly ILogger<UpdateInstanceService> _logger;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly int _largeDicomItemSizeInBytes;
    private readonly int _stageBlockSizeInBytes;
    private const int LargeObjectSizeInBytes = 1000;

    public UpdateInstanceService(
        IFileStore fileStore,
        IMetadataStore metadataStore,
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        IOptions<UpdateConfiguration> updateConfiguration,
        ILogger<UpdateInstanceService> logger)
    {
        _fileStore = EnsureArg.IsNotNull(fileStore, nameof(fileStore));
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        _recyclableMemoryStreamManager = EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));
        var configuration = EnsureArg.IsNotNull(updateConfiguration?.Value, nameof(updateConfiguration));
        _largeDicomItemSizeInBytes = configuration.LargeDicomItemsizeInBytes;
        _stageBlockSizeInBytes = configuration.StageBlockSizeInBytes;
    }

    /// <inheritdoc />
    public async Task<FileProperties> UpdateInstanceBlobAsync(InstanceMetadata instance, DicomDataset datasetToUpdate, Partition partition, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(datasetToUpdate, nameof(datasetToUpdate));
        EnsureArg.IsNotNull(instance, nameof(instance));
        EnsureArg.IsTrue(instance.InstanceProperties.NewVersion.HasValue, nameof(instance.InstanceProperties.NewVersion.HasValue));
        EnsureArg.IsNotNull(partition, nameof(partition));

        Task<FileProperties> updateInstanceFileTask = UpdateInstanceFileAsync(instance, datasetToUpdate, partition, cancellationToken);
        await UpdateInstanceMetadataAsync(instance, datasetToUpdate, cancellationToken);
        return await updateInstanceFileTask;
    }

    /// <inheritdoc />
    public async Task DeleteInstanceBlobAsync(long fileIdentifier, Partition partition, FileProperties fileProperties, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(partition, nameof(partition));
        _logger.LogInformation("Begin deleting instance blob {FileIdentifier}.", fileIdentifier);

        Task fileTask = _fileStore.DeleteFileIfExistsAsync(fileIdentifier, partition, fileProperties, cancellationToken);
        Task metadataTask = _metadataStore.DeleteInstanceMetadataIfExistsAsync(fileIdentifier, cancellationToken);

        await Task.WhenAll(fileTask, metadataTask);

        _logger.LogInformation("Deleting instance blob {FileIdentifier} completed successfully.", fileIdentifier);
    }

    private async Task<FileProperties> UpdateInstanceFileAsync(InstanceMetadata instance, DicomDataset datasetToUpdate, Partition partition, CancellationToken cancellationToken)
    {
        long originFileIdentifier = instance.VersionedInstanceIdentifier.Version;
        long newFileIdentifier = instance.InstanceProperties.NewVersion.Value;
        var stopwatch = new Stopwatch();

        KeyValuePair<string, long> block = default;
        FileProperties newFileProperties = default;

        bool isPreUpdated = instance.InstanceProperties.OriginalVersion.HasValue;

        // If the file is already updated, then we can copy the file and just update the first block
        // We don't want to download the entire file, downloading the first block is sufficient to update patient data
        if (isPreUpdated)
        {
            _logger.LogInformation("Begin copying instance file {OrignalFileIdentifier} - {NewFileIdentifier}", originFileIdentifier, newFileIdentifier);
            await _fileStore.CopyFileAsync(originFileIdentifier, newFileIdentifier, partition, instance.InstanceProperties.FileProperties, cancellationToken);
            newFileProperties = await _fileStore.GetFilePropertiesAsync(newFileIdentifier, partition, null, cancellationToken);
        }
        else
        {
            stopwatch.Start();
            _logger.LogInformation("Begin downloading original file {OrignalFileIdentifier} - {NewFileIdentifier}", originFileIdentifier, newFileIdentifier);

            // If not pre-updated get the file stream, GetFileAsync will open the stream
            using Stream stream = await _fileStore.GetFileAsync(originFileIdentifier, partition, instance.InstanceProperties.FileProperties, cancellationToken);

            // Read the file and check if there is any large DICOM item in the file.
            DicomFile dcmFile = await DicomFile.OpenAsync(stream, FileReadOption.ReadLargeOnDemand, LargeObjectSizeInBytes);

            long firstBlockLength = stream.Length;

            // If the file is greater than 1MB try to upload in different blocks.
            // Since we are supporting updating only Patient demographic and identification module,
            // we assume patient attributes are at the very beginning of the dataset.
            // If in future, we support updating other attributes like private tags, which could appear at the end
            // We need to read the whole file instead of first block.
            if (stream.Length > _largeDicomItemSizeInBytes)
            {
                _logger.LogInformation("Found bigger DICOM item, splitting the file into multiple blocks. {OrignalFileIdentifier} - {NewFileIdentifier}", originFileIdentifier, newFileIdentifier);

                if (dcmFile.Dataset.TryGetLargeDicomItem(_largeDicomItemSizeInBytes, _stageBlockSizeInBytes, out DicomItem largeDicomItem))
                {
                    RemoveItemsAfter(dcmFile.Dataset, largeDicomItem.Tag);
                }

                // Find first block length to upload in blocks
                firstBlockLength = await dcmFile.GetByteLengthAsync(_recyclableMemoryStreamManager);
            }

            // Get the block lengths for the entire file in 4 MB chunks except for the first block, which will vary depends on the large item in the dataset
            IDictionary<string, long> blockLengths = GetBlockLengths(stream.Length, firstBlockLength, _stageBlockSizeInBytes);

            _logger.LogInformation("Begin uploading instance file in blocks {OrignalFileIdentifier} - {NewFileIdentifier}", originFileIdentifier, newFileIdentifier);

            stream.Seek(0, SeekOrigin.Begin);

            // Copy the original file into another file as multiple blocks
            newFileProperties = await _fileStore.StoreFileInBlocksAsync(newFileIdentifier, partition, stream, blockLengths, cancellationToken);

            // Retain the first block information to update the metadata information
            block = blockLengths.First();

            stopwatch.Stop();

            _logger.LogInformation("Uploading instance file in blocks {FileIdentifier} completed successfully. {TotalTimeTakenInMs} ms", newFileIdentifier, stopwatch.ElapsedMilliseconds);
        }

        // Update the patient metadata only on the first block of data
        return await UpdateDatasetInFileAsync(newFileIdentifier, datasetToUpdate, partition, newFileProperties, block, cancellationToken);
    }

    private async Task UpdateInstanceMetadataAsync(InstanceMetadata instance, DicomDataset datasetToUpdate, CancellationToken cancellationToken)
    {
        long originFileIdentifier = instance.VersionedInstanceIdentifier.Version;
        long newFileIdentifier = instance.InstanceProperties.NewVersion.Value;

        _logger.LogInformation("Begin downloading original file metdata {OrignalFileIdentifier} - {NewFileIdentifier}", originFileIdentifier, newFileIdentifier);

        DicomDataset metadata = await _metadataStore.GetInstanceMetadataAsync(originFileIdentifier, cancellationToken);
        metadata.AddOrUpdate(datasetToUpdate);

        await _metadataStore.StoreInstanceMetadataAsync(metadata, newFileIdentifier, cancellationToken);

        _logger.LogInformation("Updating metadata file {OrignalFileIdentifier} - {NewFileIdentifier} completed successfully", originFileIdentifier, newFileIdentifier);
    }

    private async Task<FileProperties> UpdateDatasetInFileAsync(long newFileIdentifier, DicomDataset datasetToUpdate, Partition partition, FileProperties newFileProperties, KeyValuePair<string, long> block = default, CancellationToken cancellationToken = default)
    {
        const string SrcTag = nameof(UpdateDatasetInFileAsync) + "-src";
        const string DestTag = nameof(UpdateDatasetInFileAsync) + "-dest";

        var stopwatch = new Stopwatch();
        _logger.LogInformation("Begin updating new file {NewFileIdentifier}", newFileIdentifier);

        stopwatch.Start();

        // If the block is not provided, then we need to get the first block to update the dataset
        // This scenario occurs if the file is already updated and we have stored in multiple blocks
        if (block.Key is null)
        {
            block = await _fileStore.GetFirstBlockPropertyAsync(newFileIdentifier, partition, newFileProperties, cancellationToken);
        }

        BinaryData data = await _fileStore.GetFileContentInRangeAsync(newFileIdentifier, partition, newFileProperties, new FrameRange(0, block.Value), cancellationToken);

        using MemoryStream stream = _recyclableMemoryStreamManager.GetStream(tag: SrcTag, buffer: data);
        DicomFile dicomFile = await DicomFile.OpenAsync(stream);
        dicomFile.Dataset.AddOrUpdate(datasetToUpdate);

        using MemoryStream resultStream = _recyclableMemoryStreamManager.GetStream(tag: DestTag);
        await dicomFile.SaveAsync(resultStream);

        FileProperties updatedFileProperties = await _fileStore.UpdateFileBlockAsync(newFileIdentifier, partition, newFileProperties, block.Key, resultStream, cancellationToken);

        stopwatch.Stop();

        _logger.LogInformation("Updating new file {NewFileIdentifier} completed successfully. {TotalTimeTakenInMs} ms", newFileIdentifier, stopwatch.ElapsedMilliseconds);
        return updatedFileProperties;
    }

    internal static IDictionary<string, long> GetBlockLengths(long streamLength, long initialBlockLength, long stageBlockSizeInBytes)
    {
        var blockLengths = new Dictionary<string, long>();
        long fileSizeWithoutFirstBlock = streamLength - initialBlockLength;
        int numStagesWithoutFirstBlock = (int)Math.Ceiling((double)fileSizeWithoutFirstBlock / stageBlockSizeInBytes) + 1;

        long bytesRead = 0;
        for (int i = 0; i < numStagesWithoutFirstBlock; i++)
        {
            long blockSize = i == 0 ? initialBlockLength : Math.Min(stageBlockSizeInBytes, streamLength - bytesRead);
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
