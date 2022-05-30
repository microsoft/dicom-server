// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using FellowOakDicom.Log;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;

namespace Microsoft.Health.Dicom.Core.Features.Store;

/// <summary>
/// Provides functionality to orchestrate the storing of the DICOM instance entry.
/// </summary>
public class StoreOrchestrator : IStoreOrchestrator
{
    private readonly IDicomRequestContextAccessor _contextAccessor;
    private readonly IFileStore _fileStore;
    private readonly IMetadataStore _metadataStore;
    private readonly IIndexDataStore _indexDataStore;
    private readonly IDeleteService _deleteService;
    private readonly IQueryTagService _queryTagService;
    private readonly ILogger<StoreOrchestrator> _logger;

    public StoreOrchestrator(
        IDicomRequestContextAccessor contextAccessor,
        IFileStore fileStore,
        IMetadataStore metadataStore,
        IIndexDataStore indexDataStore,
        IDeleteService deleteService,
        IQueryTagService queryTagService,
        ILogger<StoreOrchestrator> logger)
    {
        _contextAccessor = EnsureArg.IsNotNull(contextAccessor, nameof(contextAccessor));
        _fileStore = EnsureArg.IsNotNull(fileStore, nameof(fileStore));
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _indexDataStore = EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
        _deleteService = EnsureArg.IsNotNull(deleteService, nameof(deleteService));
        _queryTagService = EnsureArg.IsNotNull(queryTagService, nameof(queryTagService));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    /// <inheritdoc />
    public async Task StoreDicomInstanceEntryAsync(
        IDicomInstanceEntry dicomInstanceEntry,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dicomInstanceEntry, nameof(dicomInstanceEntry));
        var partitionKey = _contextAccessor.RequestContext.GetPartitionKey();

        DicomDataset dicomDataset = await dicomInstanceEntry.GetDicomDatasetAsync(cancellationToken);

        IReadOnlyCollection<QueryTag> queryTags = await _queryTagService.GetQueryTagsAsync(cancellationToken: cancellationToken);
        long watermark = await _indexDataStore.BeginCreateInstanceIndexAsync(partitionKey, dicomDataset, queryTags, cancellationToken);
        var versionedInstanceIdentifier = dicomDataset.ToVersionedInstanceIdentifier(watermark);

        try
        {
            // We have successfully created the index, store the files.
            await Task.WhenAll(
                StoreFileAsync(versionedInstanceIdentifier, dicomInstanceEntry, cancellationToken),
                StoreInstanceMetadataAsync(dicomDataset, watermark, cancellationToken),
                StoreFileFramesRangeAsync(dicomDataset, watermark, cancellationToken));

            await _indexDataStore.EndCreateInstanceIndexAsync(partitionKey, dicomDataset, watermark, queryTags, cancellationToken: cancellationToken);
        }
        catch (Exception)
        {
            // Exception occurred while storing the file. Try delete the index.
            await TryCleanupInstanceIndexAsync(versionedInstanceIdentifier);
            throw;
        }
    }

    private async Task StoreFileAsync(
        VersionedInstanceIdentifier versionedInstanceIdentifier,
        IDicomInstanceEntry dicomInstanceEntry,
        CancellationToken cancellationToken)
    {
        Stream stream = await dicomInstanceEntry.GetStreamAsync(cancellationToken);

        await _fileStore.StoreFileAsync(
            versionedInstanceIdentifier,
            stream,
            cancellationToken);
    }

    private Task StoreInstanceMetadataAsync(
        DicomDataset dicomDataset,
        long version,
        CancellationToken cancellationToken)
        => _metadataStore.StoreInstanceMetadataAsync(dicomDataset, version, cancellationToken);

    private async Task StoreFileFramesRangeAsync(
            DicomDataset dicomDataset,
            long version,
            CancellationToken cancellationToken)
    {
        Dictionary<int, FrameRange> framesRange = GetFramesOffset(dicomDataset);

        if (framesRange != null && framesRange.Count > 0)
        {
            var identifier = dicomDataset.ToVersionedInstanceIdentifier(version);

            await _metadataStore.StoreInstanceFramesRangeAsync(identifier, framesRange, cancellationToken);
        }

    }
    private async Task TryCleanupInstanceIndexAsync(VersionedInstanceIdentifier versionedInstanceIdentifier)
    {
        try
        {
            // In case the request is canceled and one of the operation failed, we still want to cleanup.
            // Therefore, we will not be using the same cancellation token as the request itself.
            await _deleteService.DeleteInstanceNowAsync(
                versionedInstanceIdentifier.StudyInstanceUid,
                versionedInstanceIdentifier.SeriesInstanceUid,
                versionedInstanceIdentifier.SopInstanceUid,
                CancellationToken.None);
        }
        catch (Exception)
        {
            // Fire and forget.
        }
    }

    private Dictionary<int, FrameRange> GetFramesOffset(DicomDataset dataset)
    {
        var pixelData = dataset.GetDicomItem<DicomItem>(DicomTag.PixelData);
        int numberOfFrames = dataset.GetSingleValueOrDefault(DicomTag.NumberOfFrames, 1);

        if (numberOfFrames <= 0 && pixelData is not DicomFragmentSequence)
        {
            _logger.Info("This file has no frames");
            return null;
        }

        // todo support case where fragments != frames.
        // This means offsettable matches the frames and we have to parse the bytes in pixelData to find the right fragment and end at the right fragment.
        // there is also a 8 byte tag inbetween the fragment data that either we need to store differently or remove on the way out.
        // fo-dicom/DicomPixelData.cs/GetFrame has the logic
        var pixelDataFragment = pixelData as DicomFragmentSequence;
        if (numberOfFrames == pixelDataFragment.Fragments.Count)
        {
            var framesRange = new Dictionary<int, FrameRange>();
            for (int i = 0; i < pixelDataFragment.Fragments.Count; i++)
            {
                var fragment = pixelDataFragment.Fragments[i];
                if (fragment is StreamByteBuffer streamByteBuffer)
                {
                    framesRange.Add(i, new FrameRange(streamByteBuffer.Position, streamByteBuffer.Size));
                }
                else if (fragment is FileByteBuffer fileByteBuffer)
                {
                    framesRange.Add(i, new FrameRange(fileByteBuffer.Position, fileByteBuffer.Size));
                }
            }
            return framesRange;
        }
        else
        {
            _logger.Info("This file fragment count {0} does not match frame count {1}", pixelDataFragment.Fragments.Count, numberOfFrames);
        }

        return null;
    }
}
