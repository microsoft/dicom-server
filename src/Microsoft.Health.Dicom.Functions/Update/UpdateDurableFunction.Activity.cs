// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    /// The result of the task contains the updated instances.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(UpdateInstanceWatermarkV2Async))]
    public async Task<IEnumerable<InstanceMetadata>> UpdateInstanceWatermarkV2Async([ActivityTrigger] UpdateInstanceWatermarkArgumentsV2 arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.StudyInstanceUid, nameof(arguments.StudyInstanceUid));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Beginning to update all instance watermarks");

        IEnumerable<InstanceMetadata> instanceMetadata = await _indexStore.BeginUpdateInstancesAsync(arguments.Partition, arguments.StudyInstanceUid, CancellationToken.None);

        logger.LogInformation("Beginning to update all instance watermarks");

        return instanceMetadata;
    }

    /// <summary>
    /// Asynchronously batches the instance watermarks and calls the update instance.
    /// </summary>
    /// <param name="arguments">BatchUpdateArguments</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// The result of the task contains the updated instances with file properties representing newly created blobs.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(UpdateInstanceBlobsV2Async))]
    public async Task<IReadOnlyCollection<InstanceMetadata>> UpdateInstanceBlobsV2Async(
        [ActivityTrigger] UpdateInstanceBlobArgumentsV2 arguments,
        ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.ChangeDataset, nameof(arguments.ChangeDataset));
        EnsureArg.IsNotNull(arguments.InstanceMetadataList, nameof(arguments.InstanceMetadataList));
        EnsureArg.IsNotNull(arguments.Partition, nameof(arguments.Partition));
        EnsureArg.IsNotNull(logger, nameof(logger));

        DicomDataset datasetToUpdate = GetDeserialzedDataset(arguments.ChangeDataset);

        int processed = 0;

        logger.LogInformation("Beginning to update all instance blobs, Total count {TotalCount}", arguments.InstanceMetadataList.Count);

        ConcurrentBag<InstanceMetadata> updatedInstances = new ConcurrentBag<InstanceMetadata>();
        while (processed < arguments.InstanceMetadataList.Count)
        {
            int batchSize = Math.Min(_options.BatchSize, arguments.InstanceMetadataList.Count - processed);
            var batch = arguments.InstanceMetadataList.Skip(processed).Take(batchSize).ToList();

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
                    FileProperties fileProperties = await _updateInstanceService.UpdateInstanceBlobAsync(instance, datasetToUpdate, arguments.Partition, token);
                    updatedInstances.Add(
                        new InstanceMetadata(
                            instance.VersionedInstanceIdentifier,
                            new InstanceProperties
                            {
                                fileProperties = fileProperties,
                                NewVersion = instance.InstanceProperties.NewVersion,
                                OriginalVersion = instance.InstanceProperties.OriginalVersion
                            }));
                });

            logger.LogInformation("Completed updating instance blobs starting with [{Start}, {End}]. Total batchSize {BatchSize}.",
                batch[0],
                batch[^1],
                batchSize);

            processed += batchSize;
        }

        logger.LogInformation("Completed updating all instance blobs");
        return updatedInstances;
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
    public async Task CompleteUpdateStudyV2Async([ActivityTrigger] CompleteStudyArgumentsV2 arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.ChangeDataset, nameof(arguments.ChangeDataset));
        EnsureArg.IsNotNull(arguments.StudyInstanceUid, nameof(arguments.StudyInstanceUid));
        EnsureArg.IsNotNull(arguments.InstanceMetadataList, nameof(arguments.InstanceMetadataList));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Completing updating operation for study.");

        try
        {
            await _indexStore.EndUpdateInstanceAsync(
                arguments.PartitionKey,
                arguments.StudyInstanceUid,
                GetDeserialzedDataset(arguments.ChangeDataset),
                arguments.InstanceMetadataList,
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
    /// <param name="arguments">Activity context which has list of watermarks to cleanup</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="DeleteOldVersionBlobV2Async"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(DeleteOldVersionBlobV2Async))]
    [Obsolete("Use DeleteOldVersionBlobV3Async instead")]
    public async Task DeleteOldVersionBlobV2Async([ActivityTrigger] CleanupBlobArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.Partition, nameof(arguments.Partition));
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
                await _updateInstanceService.DeleteInstanceBlobAsync(fileIdentifier.Version, partition, null, token);
            });

        logger.LogInformation("Old blobs deleted successfully. Total size {TotalCount}", fileCount);
    }

    /// <summary>
    /// Asynchronously delete all the old blobs if it has more than 2 version.
    /// </summary>
    /// <param name="arguments">Activity context which has list of watermarks to cleanup</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="DeleteOldVersionBlobV3Async"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(DeleteOldVersionBlobV3Async))]
    public async Task DeleteOldVersionBlobV3Async([ActivityTrigger] CleanupBlobArgumentsV2 arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.Partition, nameof(arguments.Partition));
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(arguments.Instances, nameof(arguments.Instances));

        IReadOnlyList<InstanceMetadata> instances = arguments.Instances;
        Partition partition = arguments.Partition;
        int fileCount = instances.Where(i => i.InstanceProperties.OriginalVersion.HasValue).Count();

        logger.LogInformation("Begin deleting old blobs. Total size {TotalCount}", fileCount);

        await Parallel.ForEachAsync(
            instances.Where(i => i.InstanceProperties.OriginalVersion.HasValue),
            new ParallelOptions
            {
                CancellationToken = default,
                MaxDegreeOfParallelism = _options.MaxParallelThreads,
            },
            async (instance, token) =>
            {
                await _updateInstanceService.DeleteInstanceBlobAsync(instance.VersionedInstanceIdentifier.Version, partition, instance.InstanceProperties.fileProperties, token);
            });

        logger.LogInformation("Old blobs deleted successfully. Total size {TotalCount}", fileCount);
    }

    /// <summary>
    /// Asynchronously delete the new blob when there is a failure while updating the study instances.
    /// </summary>
    /// <param name="arguments">arguments which have a list of watermarks to cleanup along with partition they belong to</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="CleanupNewVersionBlobV3Async"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(CleanupNewVersionBlobV3Async))]
    public async Task CleanupNewVersionBlobV3Async([ActivityTrigger] CleanupBlobArgumentsV2 arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.Partition, nameof(arguments.Partition));
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(arguments.Instances, nameof(arguments.Instances));

        IReadOnlyList<InstanceMetadata> instances = arguments.Instances;
        Partition partition = arguments.Partition;

        int fileCount = instances.Where(instance => instance.InstanceProperties.NewVersion.HasValue).Count();
        logger.LogInformation("Begin cleaning up new blobs. Total size {TotalCount}", fileCount);

        await Parallel.ForEachAsync(
            instances.Where(instance => instance.InstanceProperties.NewVersion.HasValue),
            new ParallelOptions
            {
                CancellationToken = default,
                MaxDegreeOfParallelism = _options.MaxParallelThreads,
            },
            async (instance, token) =>
            {
                await _updateInstanceService.DeleteInstanceBlobAsync(instance.InstanceProperties.NewVersion.Value, partition, instance.InstanceProperties.fileProperties, token);
            });

        logger.LogInformation("New blobs deleted successfully. Total size {TotalCount}", fileCount);
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
    [Obsolete("Use CleanupNewVersionBlobV3Async instead")]
    public async Task CleanupNewVersionBlobV2Async([ActivityTrigger] CleanupBlobArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.Partition, nameof(arguments.Partition));
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
                await _updateInstanceService.DeleteInstanceBlobAsync(fileIdentifier.NewVersion.Value, partition, null, token);
            });

        logger.LogInformation("New blobs deleted successfully. Total size {TotalCount}", fileCount);
    }

    /// <summary>
    /// Asynchronously move all the original version blobs to cold access tier.
    /// </summary>
    /// <param name="arguments">arguments which have a list of watermarks to move to cold access tier along with partition they belong to</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="SetOriginalBlobToColdAccessTierAsync"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(SetOriginalBlobToColdAccessTierAsync))]
    [Obsolete("Use SetOriginalBlobToColdAccessTierV2Async instead")]
    public async Task SetOriginalBlobToColdAccessTierAsync([ActivityTrigger] CleanupBlobArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.Partition, nameof(arguments.Partition));
        EnsureArg.IsNotNull(logger, nameof(logger));

        IReadOnlyList<InstanceFileState> fileIdentifiers = arguments.InstanceWatermarks;
        Partition partition = arguments.Partition;

        int fileCount = fileIdentifiers.Where(f => f.NewVersion.HasValue && !f.OriginalVersion.HasValue).Count();
        logger.LogInformation("Begin moving original version blob from hot to cold access tier. Total size {TotalCount}", fileCount);

        // Set to cold tier only for first time update, not for subsequent updates. This is to avoid moving the blob to cold tier multiple times.
        // If the original version is set, then it means that the instance is updated already.
        await Parallel.ForEachAsync(
           fileIdentifiers.Where(f => f.NewVersion.HasValue && !f.OriginalVersion.HasValue),
           new ParallelOptions
           {
               CancellationToken = default,
               MaxDegreeOfParallelism = _options.MaxParallelThreads,
           },
           async (fileIdentifier, token) =>
           {
               await _fileStore.SetBlobToColdAccessTierAsync(fileIdentifier.Version, partition, null, token);
           });

        logger.LogInformation("Original version blob is moved to cold access tier successfully. Total size {TotalCount}", fileCount);
    }

    /// <summary>
    /// Asynchronously move all the original version blobs to cold access tier.
    /// </summary>
    /// <param name="arguments">arguments which have a list of watermarks to move to cold access tier along with partition they belong to</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="SetOriginalBlobToColdAccessTierV2Async"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(SetOriginalBlobToColdAccessTierV2Async))]
    public async Task SetOriginalBlobToColdAccessTierV2Async([ActivityTrigger] CleanupBlobArgumentsV2 arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.Partition, nameof(arguments.Partition));
        EnsureArg.IsNotNull(logger, nameof(logger));

        IReadOnlyList<InstanceMetadata> instances = arguments.Instances;
        Partition partition = arguments.Partition;

        int fileCount = instances.Where(i => i.InstanceProperties.NewVersion.HasValue && !i.InstanceProperties.OriginalVersion.HasValue).Count();
        logger.LogInformation("Begin moving original version blob from hot to cold access tier. Total size {TotalCount}", fileCount);

        // Set to cold tier only for first time update, not for subsequent updates. This is to avoid moving the blob to cold tier multiple times.
        // If the original version is set, then it means that the instance is updated already.
        await Parallel.ForEachAsync(
           instances.Where(i => i.InstanceProperties.NewVersion.HasValue && !i.InstanceProperties.OriginalVersion.HasValue),
           new ParallelOptions
           {
               CancellationToken = default,
               MaxDegreeOfParallelism = _options.MaxParallelThreads,
           },
           async (instance, token) =>
           {
               await _fileStore.SetBlobToColdAccessTierAsync(instance.VersionedInstanceIdentifier.Version, partition, instance.InstanceProperties.fileProperties, token);
           });

        logger.LogInformation("Original version blob is moved to cold access tier successfully. Total size {TotalCount}", fileCount);
    }

    private DicomDataset GetDeserialzedDataset(string dataset) => JsonSerializer.Deserialize<DicomDataset>(dataset, _jsonSerializerOptions);
}
