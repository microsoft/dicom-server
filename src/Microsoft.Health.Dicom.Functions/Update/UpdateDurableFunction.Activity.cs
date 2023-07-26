// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Functions.Update.Models;

namespace Microsoft.Health.Dicom.Functions.Update;

public partial class UpdateDurableFunction
{
    /// <summary>
    /// Asynchronously update instance new watermark.
    /// </summary>
    /// <param name="arguments">BatchUpdateArguments</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="UpdateInstanceWatermarkAsync"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(UpdateInstanceWatermarkAsync))]
    public async Task<IReadOnlyList<InstanceFileState>> UpdateInstanceWatermarkAsync([ActivityTrigger] UpdateInstanceWatermarkArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.StudyInstanceUid, nameof(arguments.StudyInstanceUid));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Beginning to update all instance watermarks");

        IEnumerable<InstanceMetadata> instanceMetadata = await _indexStore.BeginUpdateInstancesAsync(arguments.Partition, arguments.StudyInstanceUid, CancellationToken.None);

        logger.LogInformation("Beginning to update all instance watermarks");

        return instanceMetadata.Select(x =>
            new InstanceFileState
            {
                Version = x.VersionedInstanceIdentifier.Version,
                OriginalVersion = x.InstanceProperties.OriginalVersion,
                NewVersion = x.InstanceProperties.NewVersion
            }).ToList();
    }

    /// <summary>
    /// Asynchronously batches the instance watermarks and calls the update instance.
    /// </summary>
    /// <param name="arguments">BatchUpdateArguments</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="UpdateInstanceBlobsAsync"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(UpdateInstanceBlobsAsync))]
    public async Task<IReadOnlyList<WatermarkedFileProperties>> UpdateInstanceBlobsAsync([ActivityTrigger] UpdateInstanceBlobArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.ChangeDataset, nameof(arguments.ChangeDataset));
        EnsureArg.IsNotNull(arguments.InstanceWatermarks, nameof(arguments.InstanceWatermarks));
        EnsureArg.IsNotNull(logger, nameof(logger));

        DicomDataset datasetToUpdate = GetDeserialzedDataset(arguments.ChangeDataset);

        int processed = 0;

        logger.LogInformation("Beginning to update all instance blobs, Total count {TotalCount}", arguments.InstanceWatermarks.Count);

        List<WatermarkedFileProperties> propertiesByWatermark = new List<WatermarkedFileProperties>();
        while (processed < arguments.InstanceWatermarks.Count)
        {
            int batchSize = Math.Min(_options.BatchSize, arguments.InstanceWatermarks.Count - processed);
            var batch = arguments.InstanceWatermarks.Skip(processed).Take(batchSize).ToList();

            logger.LogInformation("Beginning to update instance blobs for range [{Start}, {End}]. Total batch size {BatchSize}.",
                    batch[0],
                    batch[^1],
                    batchSize);

            await Parallel.ForEachAsync(
                batch,
                new ParallelOptions
                {
                    CancellationToken = default,
                    MaxDegreeOfParallelism = _options.MaxParallelThreads,
                },
                async (instance, token) =>
                {
                    long newFileIdentifier = instance.NewVersion.Value;
                    FileProperties fileProperties = await _updateInstanceService.UpdateInstanceBlobAsync(instance, datasetToUpdate, arguments.Partition, token);
                    if (fileProperties is not null)
                    {
                        propertiesByWatermark.Add(new WatermarkedFileProperties
                        {
                            Watermark = newFileIdentifier,
                            ContentLength = fileProperties.ContentLength,
                            ETag = fileProperties.ETag,
                            Path = fileProperties.Path
                        });
                    }
                });

            logger.LogInformation("Completed updating instance blobs starting with [{Start}, {End}]. Total batchSize {BatchSize}.",
                batch[0],
                batch[^1],
                batchSize);

            processed += batchSize;
        }

        logger.LogInformation("Completed updating all instance blobs");
        return new ReadOnlyCollection<WatermarkedFileProperties>(propertiesByWatermark);
    }

    /// <summary>
    /// Asynchronously commits all the instances in a study and creates new entries for changefeed.
    /// </summary>
    /// <param name="arguments">CompleteInstanceArguments</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="CompleteUpdateStudyAsync"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [Obsolete("Please use UpdateInstanceBlobsV2Async instead.")]
    [FunctionName(nameof(CompleteUpdateStudyAsync))]
    public async Task CompleteUpdateStudyAsync([ActivityTrigger] CompleteStudyArguments arguments, ILogger logger)
    {
        await CompleteUpdateStudyV2Async(arguments, logger);
    }

    /// <summary>
    /// Asynchronously commits all the instances in a study and creates new entries for changefeed.
    /// </summary>
    /// <param name="arguments">CompleteInstanceArguments</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="CompleteUpdateStudyV2Async"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(CompleteUpdateStudyV2Async))]
    public async Task CompleteUpdateStudyV2Async([ActivityTrigger] CompleteStudyArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.ChangeDataset, nameof(arguments.ChangeDataset));
        EnsureArg.IsNotNull(arguments.StudyInstanceUid, nameof(arguments.StudyInstanceUid));
        EnsureArg.IsNotNull(arguments.WatermarkedFilePropertiesList, nameof(arguments.WatermarkedFilePropertiesList));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Completing updating operation for study.");

        try
        {
            await _indexStore.EndUpdateInstanceAsync(
                arguments.PartitionKey,
                arguments.StudyInstanceUid,
                GetDeserialzedDataset(arguments.ChangeDataset),
                arguments.WatermarkedFilePropertiesList,
                CancellationToken.None);

            logger.LogInformation("Updating study completed successfully.");
        }
        catch (StudyNotFoundException)
        {
            // TODO: Study deleted, we need to cleanup all the newly updated blobs.
            logger.LogWarning("Failed to update to study. Possibly deleted.");
        }
    }

    /// <summary>
    /// Asynchronously delete all the old blobs if it has more than 2 version.
    /// </summary>
    /// <param name="context">Activity context which has list of watermarks to cleanup</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="DeleteOldVersionBlobAsync"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(DeleteOldVersionBlobAsync))]
    public async Task DeleteOldVersionBlobAsync([ActivityTrigger] IDurableActivityContext context, ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        IReadOnlyList<InstanceFileState> fileIdentifiers = context.GetInput<IReadOnlyList<InstanceFileState>>();
        await DeleteOldVersionBlobV2Async(new CleanupNewVersionBlobArguments(fileIdentifiers, Partition.Default), logger);
    }

    /// <summary>
    /// Asynchronously delete all the old blobs if it has more than 2 version.
    /// </summary>
    /// <param name="arguments">Activity context which has list of watermarks to cleanup</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="DeleteOldVersionBlobV2Async"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(DeleteOldVersionBlobV2Async))]
    public async Task DeleteOldVersionBlobV2Async([ActivityTrigger] CleanupNewVersionBlobArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(logger, nameof(logger));

        IReadOnlyList<InstanceFileState> fileIdentifiers = arguments.InstanceWatermarks;
        Partition partition = arguments.Partition;
        int fileCount = fileIdentifiers.Where(f => f.OriginalVersion.HasValue).Count();

        logger.LogInformation("Begin deleting old blobs. Total size {TotalCount}", fileCount);

        await Parallel.ForEachAsync(
            fileIdentifiers.Where(f => f.OriginalVersion.HasValue),
            new ParallelOptions
            {
                CancellationToken = default,
                MaxDegreeOfParallelism = _options.MaxParallelThreads,
            },
            async (fileIdentifier, token) =>
            {
                await _updateInstanceService.DeleteInstanceBlobAsync(fileIdentifier.Version, partition, token);
            });

        logger.LogInformation("Old blobs deleted successfully. Total size {TotalCount}", fileCount);
    }

    /// <summary>
    /// Asynchronously delete the new blob when there is a failure while updating the study instances.
    /// </summary>
    /// <param name="context">Activity context which has list of watermarks to cleanup</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="CleanupNewVersionBlobAsync"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [Obsolete("Please use CleanupNewVersionBlobV2Async instead.")]
    [FunctionName(nameof(CleanupNewVersionBlobAsync))]
    public async Task CleanupNewVersionBlobAsync([ActivityTrigger] IDurableActivityContext context, ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        await CleanupNewVersionBlobV2Async(
            new CleanupNewVersionBlobArguments(context.GetInput<IReadOnlyList<InstanceFileState>>(), Partition.Default),
            logger);
    }

    /// <summary>
    /// Asynchronously delete the new blob when there is a failure while updating the study instances.
    /// </summary>
    /// <param name="arguments">arguments which have a list of watermarks to cleanup along with partition they belong to</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="CleanupNewVersionBlobV2Async"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(CleanupNewVersionBlobV2Async))]
    public async Task CleanupNewVersionBlobV2Async([ActivityTrigger] CleanupNewVersionBlobArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(logger, nameof(logger));

        IReadOnlyList<InstanceFileState> fileIdentifiers = arguments.InstanceWatermarks;
        Partition partition = arguments.Partition;

        int fileCount = fileIdentifiers.Where(f => f.NewVersion.HasValue).Count();
        logger.LogInformation("Begin cleaning up new blobs. Total size {TotalCount}", fileCount);

        await Parallel.ForEachAsync(
            fileIdentifiers.Where(f => f.NewVersion.HasValue),
            new ParallelOptions
            {
                CancellationToken = default,
                MaxDegreeOfParallelism = _options.MaxParallelThreads,
            },
            async (fileIdentifier, token) =>
            {
                await _updateInstanceService.DeleteInstanceBlobAsync(fileIdentifier.NewVersion.Value, partition, token);
            });

        logger.LogInformation("New blobs deleted successfully. Total size {TotalCount}", fileCount);
    }

    private DicomDataset GetDeserialzedDataset(string dataset) => JsonSerializer.Deserialize<DicomDataset>(dataset, _jsonSerializerOptions);
}
