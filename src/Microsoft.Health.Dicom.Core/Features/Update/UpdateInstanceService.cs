// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.IO;
using FellowOakDicom.IO.Writer;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core.Features.Update;
public class UpdateInstanceService : IUpdateInstanceService
{
    private readonly IFileStore _fileStore;
    private readonly IMetadataStore _metadataStore;
    private readonly ILogger<UpdateInstanceService> _logger;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private const int LargeObjectsizeInBytes = 1024 * 1024;

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

    public async Task UpdateInstanceBlobAsync(long fileIdentifier, long newFileIdentifier, DicomDataset datasetToUpdate, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(datasetToUpdate, nameof(datasetToUpdate));

        Task updateInstanceFileTask = UpdateInstanceFileAsync(fileIdentifier, newFileIdentifier, datasetToUpdate, cancellationToken);
        Task updateInstanceMetadataTask = UpdateInstanceMetadataAsync(fileIdentifier, newFileIdentifier, datasetToUpdate, cancellationToken);
        await Task.WhenAll(updateInstanceFileTask, updateInstanceMetadataTask);
    }

    private async Task UpdateInstanceFileAsync(long fileIdentifier, long newFileIdentifier, DicomDataset datasetToUpdate, CancellationToken cancellationToken)
    {
        Stream stream = await _fileStore.GetStreamingFileAsync(fileIdentifier, cancellationToken);
        var dcmFile = await DicomFile.OpenAsync(stream, FileReadOption.ReadLargeOnDemand);
        var datasetSize = TryGetDatasetSize(dcmFile.Dataset, out DicomItem largeDicomItem);

        if (largeDicomItem != null)
            RemoveItemsFromDataset(dcmFile.Dataset, largeDicomItem);

        var firstBlockLength = await GetFirstBlockLengthAsync(dcmFile);
    }

    private async Task UpdateInstanceMetadataAsync(long fileIdentifier, long newFileIdentifier, DicomDataset datasetToUpdate, CancellationToken cancellationToken)
    {
        DicomDataset metadata = await _metadataStore.GetInstanceMetadataAsync(fileIdentifier, cancellationToken);
        metadata.AddOrUpdate(datasetToUpdate);

        await _metadataStore.StoreInstanceMetadataAsync(metadata, newFileIdentifier, cancellationToken);
    }

    private static long TryGetDatasetSize(DicomDataset dataset, out DicomItem largeDicomItem)
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
                    sequenceSize += TryGetDatasetSize(sequenceItem, out largeDicomItem);
                    totalSize += sequenceSize;
                }

                if (sequenceSize > LargeObjectsizeInBytes)
                {
                    largeDicomItem = item;
                    break;
                }
            }
            else
            {
                // TODO: log error or fallback to original logic
            }
        }

        return totalSize;
    }

    private async Task<long> GetFirstBlockLengthAsync(DicomFile dcmFile)
    {
        var dataset = dcmFile.Dataset;

        DicomWriteOptions writeOptions = new DicomWriteOptions();
        MemoryStream resultStream = _recyclableMemoryStreamManager.GetStream();

        var target = new StreamByteTarget(resultStream);

        DicomFileWriter writer = new DicomFileWriter(writeOptions);
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
