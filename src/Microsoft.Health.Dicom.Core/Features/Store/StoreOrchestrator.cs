// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
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
    private readonly bool _isExternalStoreEnabled;

    public StoreOrchestrator(
        IDicomRequestContextAccessor contextAccessor,
        IFileStore fileStore,
        IMetadataStore metadataStore,
        IIndexDataStore indexDataStore,
        IDeleteService deleteService,
        IQueryTagService queryTagService,
        IOptions<FeatureConfiguration> featureConfiguration,
        ILogger<StoreOrchestrator> logger)
    {
        _contextAccessor = EnsureArg.IsNotNull(contextAccessor, nameof(contextAccessor));
        _fileStore = EnsureArg.IsNotNull(fileStore, nameof(fileStore));
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _indexDataStore = EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
        _deleteService = EnsureArg.IsNotNull(deleteService, nameof(deleteService));
        _queryTagService = EnsureArg.IsNotNull(queryTagService, nameof(queryTagService));
        EnsureArg.IsNotNull(featureConfiguration, nameof(featureConfiguration));
        _isExternalStoreEnabled = EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration)).EnableExternalStore;
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

        var partition = _contextAccessor.RequestContext.GetPartition();

        string dicomInstanceIdentifier = dicomDataset.ToInstanceIdentifier(partition).ToString();

        _logger.LogInformation("Storing a DICOM instance: '{DicomInstance}'.", dicomInstanceIdentifier);

        IReadOnlyCollection<QueryTag> queryTags = await _queryTagService.GetQueryTagsAsync(cancellationToken: cancellationToken);
        long version = await _indexDataStore.BeginCreateInstanceIndexAsync(partition, dicomDataset, queryTags, cancellationToken);
        var versionedInstanceIdentifier = dicomDataset.ToVersionedInstanceIdentifier(version, partition);

        try
        {
            // We have successfully created the index, store the files.
            Task<FileProperties> storeFileTask = StoreFileAsync(versionedInstanceIdentifier, partition.Name, dicomInstanceEntry, cancellationToken);
            Task<bool> frameRangeTask = StoreFileFramesRangeAsync(dicomDataset, version, cancellationToken);
            await Task.WhenAll(
                storeFileTask,
                StoreInstanceMetadataAsync(dicomDataset, version, cancellationToken),
                frameRangeTask);

            FileProperties fileProperties = await storeFileTask;

            bool hasFrameMetadata = await frameRangeTask;

            await _indexDataStore.EndCreateInstanceIndexAsync(partition.Key, dicomDataset, version, queryTags, ShouldStoreFileProperties(fileProperties), hasFrameMetadata: hasFrameMetadata, cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully stored the DICOM instance: '{DicomInstance}'.", dicomInstanceIdentifier);

            return fileProperties.ContentLength;
        }
        catch (Exception)
        {
            _logger.LogWarning("Failed to store the DICOM instance: '{DicomInstance}'.", dicomInstanceIdentifier);

            // Exception occurred while storing the file. Try delete the index.
            await TryCleanupInstanceIndexAsync(versionedInstanceIdentifier);
            throw;
        }
    }

    private FileProperties ShouldStoreFileProperties(FileProperties fileProperties)
    {
        return _isExternalStoreEnabled ? fileProperties : null;
    }

    private async Task<FileProperties> StoreFileAsync(
        VersionedInstanceIdentifier versionedInstanceIdentifier,
        string partitionName,
        IDicomInstanceEntry dicomInstanceEntry,
        CancellationToken cancellationToken)
    {
        Stream stream = await dicomInstanceEntry.GetStreamAsync(cancellationToken);

        return await _fileStore.StoreFileAsync(versionedInstanceIdentifier.Version, partitionName, stream, cancellationToken);
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
            await _metadataStore.StoreInstanceFramesRangeAsync(version, framesRange, cancellationToken);
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

        return framesRange.Count > 0 ? framesRange : null;
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
