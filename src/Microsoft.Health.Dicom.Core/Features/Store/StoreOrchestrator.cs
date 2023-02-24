// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
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
    public async Task<long> StoreDicomInstanceEntryAsync(
        IDicomInstanceEntry dicomInstanceEntry,
        CancellationToken cancellationToken
        )
    {
        EnsureArg.IsNotNull(dicomInstanceEntry, nameof(dicomInstanceEntry));

        DicomDataset dicomDataset = await dicomInstanceEntry.GetDicomDatasetAsync(cancellationToken);

        string dicomInstanceIdentifier = dicomDataset.ToInstanceIdentifier().ToString();

        _logger.LogInformation("Storing a DICOM instance: '{DicomInstance}'.", dicomInstanceIdentifier);

        var partitionKey = _contextAccessor.RequestContext.GetPartitionKey();

        IReadOnlyCollection<QueryTag> queryTags = await _queryTagService.GetQueryTagsAsync(cancellationToken: cancellationToken);
        long watermark = await _indexDataStore.BeginCreateInstanceIndexAsync(partitionKey, dicomDataset, queryTags, cancellationToken);
        var versionedInstanceIdentifier = dicomDataset.ToVersionedInstanceIdentifier(watermark);

        try
        {
            // We have successfully created the index, store the files.
            Task<long> storeFileTask = StoreFileAsync(versionedInstanceIdentifier, dicomInstanceEntry, cancellationToken);
            Task<bool> frameRangeTask = StoreFileFramesRangeAsync(dicomDataset, watermark, cancellationToken);
            await Task.WhenAll(
                storeFileTask,
                StoreInstanceMetadataAsync(dicomDataset, watermark, cancellationToken),
                frameRangeTask);

            bool hasFrameMetadata = await frameRangeTask;

            await _indexDataStore.EndCreateInstanceIndexAsync(partitionKey, dicomDataset, watermark, queryTags, hasFrameMetadata: hasFrameMetadata, cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully stored the DICOM instance: '{DicomInstance}'.", dicomInstanceIdentifier);
            return await storeFileTask;
        }
        catch (Exception)
        {
            _logger.LogWarning("Failed to store the DICOM instance: '{DicomInstance}'.", dicomInstanceIdentifier);

            // Exception occurred while storing the file. Try delete the index.
            await TryCleanupInstanceIndexAsync(versionedInstanceIdentifier);
            throw;
        }
    }

    private async Task<long> StoreFileAsync(
        VersionedInstanceIdentifier versionedInstanceIdentifier,
        IDicomInstanceEntry dicomInstanceEntry,
        CancellationToken cancellationToken)
    {
        Stream stream = await dicomInstanceEntry.GetStreamAsync(cancellationToken);

        await _fileStore.StoreFileAsync(
            versionedInstanceIdentifier,
            stream,
            cancellationToken);

        return stream.Length;
    }

    private Task StoreInstanceMetadataAsync(
        DicomDataset dicomDataset,
        long version,
        CancellationToken cancellationToken)
        => _metadataStore.StoreInstanceMetadataAsync(dicomDataset, version, cancellationToken);

    private async Task<bool> StoreFileFramesRangeAsync(
            DicomDataset dicomDataset,
            long version,
            CancellationToken cancellationToken)
    {
        bool hasFrameMetadata = false;
        Dictionary<int, FrameRange> framesRange = GetFramesOffset(dicomDataset);

        if (framesRange != null && framesRange.Count > 0)
        {
            var identifier = dicomDataset.ToVersionedInstanceIdentifier(version);

            await _metadataStore.StoreInstanceFramesRangeAsync(identifier, framesRange, cancellationToken);
            hasFrameMetadata = true;
        }
        return hasFrameMetadata;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Method will not throw.")]
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

    internal static Dictionary<int, FrameRange> GetFramesOffset(DicomDataset dataset)
    {
        if (!dataset.TryGetPixelData(out DicomPixelData dicomPixel))
        {
            return null;
        }

        if (dicomPixel.NumberOfFrames < 1)
        {
            return null;
        }

        var pixelData = dataset.GetDicomItem<DicomItem>(DicomTag.PixelData);
        var framesRange = new Dictionary<int, FrameRange>();

        // todo support case where fragments != frames.
        // This means offsettable matches the frames and we have to parse the bytes in pixelData to find the right fragment and end at the right fragment.
        // there is also a 8 byte tag inbetween the fragment data that we need to handlee.
        // fo-dicom/DicomPixelData.cs/GetFrame has the logic
        if (pixelData is DicomFragmentSequence pixelDataFragment
            && pixelDataFragment.Fragments.Count == dicomPixel.NumberOfFrames)
        {
            for (int i = 0; i < pixelDataFragment.Fragments.Count; i++)
            {
                var fragment = pixelDataFragment.Fragments[i];
                if (TryGetBufferPosition(fragment, out long position, out long size))
                {
                    framesRange.Add(i, new FrameRange(position, size));
                }
            }
        }
        else if (pixelData is DicomOtherByte)
        {
            var dicomPixelOtherByte = dataset.GetDicomItem<DicomOtherByte>(DicomTag.PixelData);

            for (int i = 0; i < dicomPixel.NumberOfFrames; i++)
            {
                IByteBuffer byteBuffer = dicomPixel.GetFrame(i);
                if (TryGetBufferPosition(dicomPixelOtherByte.Buffer, out long position, out long size)
                    && byteBuffer is RangeByteBuffer rangeByteBuffer)
                {
                    framesRange.Add(i, new FrameRange(position + rangeByteBuffer.Offset, rangeByteBuffer.Length));
                }
            }
        }
        else if (pixelData is DicomOtherWord)
        {
            var dicomPixelWordByte = dataset.GetDicomItem<DicomOtherWord>(DicomTag.PixelData);

            for (int i = 0; i < dicomPixel.NumberOfFrames; i++)
            {
                IByteBuffer byteBuffer = dicomPixel.GetFrame(i);
                if (TryGetBufferPosition(dicomPixelWordByte.Buffer, out long position, out long size)
                    && byteBuffer is RangeByteBuffer rangeByteBuffer)
                {
                    framesRange.Add(i, new FrameRange(position + rangeByteBuffer.Offset, rangeByteBuffer.Length));
                }
            }
        }

        if (framesRange.Any())
        {
            return framesRange;
        }

        return null;
    }

    private static bool TryGetBufferPosition(IByteBuffer buffer, out long position, out long size)
    {
        bool result = false;
        position = 0;
        size = 0;
        if (buffer is StreamByteBuffer streamByteBuffer)
        {
            position = streamByteBuffer.Position;
            size = streamByteBuffer.Size;
            result = true;
        }
        else if (buffer is FileByteBuffer fileByteBuffer)
        {
            position = fileByteBuffer.Position;
            size = fileByteBuffer.Size;
            result = true;
        }
        return result;
    }
}
