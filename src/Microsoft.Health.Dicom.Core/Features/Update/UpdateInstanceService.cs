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
    public async Task UpdateInstanceBlobAsync(InstanceFileState instanceFileIdentifier, DicomDataset datasetToUpdate, CancellationToken cancellationToken)
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

        // Note: External store not supported with update yet. Pass in partition name once supported
        Task fileTask = _fileStore.DeleteFileIfExistsAsync(fileIdentifier, String.Empty, cancellationToken);
        Task metadataTask = _metadataStore.DeleteInstanceMetadataIfExistsAsync(fileIdentifier, cancellationToken);

        await Task.WhenAll(fileTask, metadataTask);

        _logger.LogInformation("Deleting instance blob {FileIdentifier} completed successfully.", fileIdentifier);
    }

    private async Task UpdateInstanceFileAsync(InstanceFileState instanceFileIdentifier, DicomDataset datasetToUpdate, CancellationToken cancellationToken)
    {
        long originFileIdentifier = instanceFileIdentifier.Version;
        long newFileIdentifier = instanceFileIdentifier.NewVersion.Value;
        var stopwatch = new Stopwatch();

        KeyValuePair<string, long> block = default;

        bool isPreUpdated = instanceFileIdentifier.OriginalVersion.HasValue;

        // If the file is already updated, then we can copy the file and just update the first block
        // We don't want to download the entire file, downloading the first block is sufficient to update patient data
        if (isPreUpdated)
        {
            _logger.LogInformation("Begin copying instance file {OrignalFileIdentifier} - {NewFileIdentifier}", originFileIdentifier, newFileIdentifier);
            await _fileStore.CopyFileAsync(originFileIdentifier, newFileIdentifier, cancellationToken);
        }
        else
        {
            stopwatch.Start();
            _logger.LogInformation("Begin downloading original file {OrignalFileIdentifier} - {NewFileIdentifier}", originFileIdentifier, newFileIdentifier);

            // If not pre-updated get the file stream, GetFileAsync will open the stream
            // // pass in partition name once update supported by external store #104373
            using Stream stream = await _fileStore.GetFileAsync(originFileIdentifier, string.Empty, cancellationToken);

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
            IDictionary<string, long> blockLengths = GetBlockLengths(stream.Length, firstBlockLength);

            _logger.LogInformation("Begin uploading instance file in blocks {OrignalFileIdentifier} - {NewFileIdentifier}", originFileIdentifier, newFileIdentifier);

            stream.Seek(0, SeekOrigin.Begin);

            // Copy the original file into another file as multiple blocks
            await _fileStore.StoreFileInBlocksAsync(newFileIdentifier, stream, blockLengths, cancellationToken);

            // Retain the first block information to update the metadata information
            block = blockLengths.First();

            stopwatch.Stop();

            _logger.LogInformation("Uploading instance file in blocks {FileIdentifier} completed successfully. {TotalTimeTakenInMs} ms", newFileIdentifier, stopwatch.ElapsedMilliseconds);
        }

        // Update the patient metadata only on the first block of data
        await UpdateDatasetInFileAsync(newFileIdentifier, datasetToUpdate, block, cancellationToken);
    }

    private async Task UpdateInstanceMetadataAsync(InstanceFileState instanceFileIdentifier, DicomDataset datasetToUpdate, CancellationToken cancellationToken)
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
        const string SrcTag = nameof(UpdateDatasetInFileAsync) + "-src";
        const string DestTag = nameof(UpdateDatasetInFileAsync) + "-dest";

        var stopwatch = new Stopwatch();
        _logger.LogInformation("Begin updating new file {NewFileIdentifier}", newFileIdentifier);

        stopwatch.Start();

        // If the block is not provided, then we need to get the first block to update the dataset
        // This scenario occurs if the file is already updated and we have stored in multiple blocks
        if (block.Key is null)
        {
            block = await _fileStore.GetFirstBlockPropertyAsync(newFileIdentifier, cancellationToken);
        }

        BinaryData data = await _fileStore.GetFileContentInRangeAsync(newFileIdentifier, new FrameRange(0, block.Value), cancellationToken);

        using MemoryStream stream = _recyclableMemoryStreamManager.GetStream(tag: SrcTag, buffer: data);
        DicomFile dicomFile = await DicomFile.OpenAsync(stream);
        dicomFile.Dataset.AddOrUpdate(datasetToUpdate);

        using MemoryStream resultStream = _recyclableMemoryStreamManager.GetStream(tag: DestTag);
        await dicomFile.SaveAsync(resultStream);

        await _fileStore.UpdateFileBlockAsync(newFileIdentifier, block.Key, resultStream, cancellationToken);

        stopwatch.Stop();

        _logger.LogInformation("Updating new file {NewFileIdentifier} completed successfully. {TotalTimeTakenInMs} ms", newFileIdentifier, stopwatch.ElapsedMilliseconds);
    }

    private IDictionary<string, long> GetBlockLengths(long streamLength, long initialBlockLength)
    {
        var blockLengths = new Dictionary<string, long>();
        long fileSizeWithoutFirstBlock = streamLength - initialBlockLength;
        int numStagesWithoutFirstBlock = (int)Math.Ceiling((double)fileSizeWithoutFirstBlock / _stageBlockSizeInBytes) + 1;

        long bytesRead = 0;
        for (int i = 0; i < numStagesWithoutFirstBlock; i++)
        {
            long blockSize = i == 0 ? initialBlockLength : Math.Min(_stageBlockSizeInBytes, streamLength - bytesRead);
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
