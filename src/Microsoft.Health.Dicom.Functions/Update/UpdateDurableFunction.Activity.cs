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
    [Obsolete("This function is obsolete. Use UpdateInstanceBlobsV3Async instead.")]
    public async Task<IReadOnlyCollection<InstanceMetadata>> UpdateInstanceBlobsV2Async(
        [ActivityTrigger] UpdateInstanceBlobArgumentsV2 arguments,
        ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.ChangeDataset, nameof(arguments.ChangeDataset));
        EnsureArg.IsNotNull(arguments.InstanceMetadataList, nameof(arguments.InstanceMetadataList));
        EnsureArg.IsNotNull(arguments.Partition, nameof(arguments.Partition));
        EnsureArg.IsNotNull(logger, nameof(logger));

        DicomDataset datasetToUpdate = GetDeserializedDataset(arguments.ChangeDataset);

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
                                FileProperties = fileProperties,
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
    /// Asynchronously batches the instance watermarks and calls the update instance.
    /// </summary>
    /// <param name="arguments">BatchUpdateArguments</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// The result of the task contains the updated instances with file properties representing newly created blobs and any error.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(UpdateInstanceBlobsV3Async))]
    public async Task<UpdateInstanceResponse> UpdateInstanceBlobsV3Async(
        [ActivityTrigger] UpdateInstanceBlobArgumentsV2 arguments,
        ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.ChangeDataset, nameof(arguments.ChangeDataset));
        EnsureArg.IsNotNull(arguments.InstanceMetadataList, nameof(arguments.InstanceMetadataList));
        EnsureArg.IsNotNull(arguments.Partition, nameof(arguments.Partition));
        EnsureArg.IsNotNull(logger, nameof(logger));

        DicomDataset datasetToUpdate = GetDeserializedDataset(arguments.ChangeDataset);

        logger.LogInformation("Beginning to update all instance blobs, Total count {TotalCount}", arguments.InstanceMetadataList.Count);

        var updatedInstances = new ConcurrentBag<InstanceMetadata>();
        var errors = new ConcurrentBag<string>();

        await Parallel.ForEachAsync(
            arguments.InstanceMetadataList,
            new ParallelOptions
            {
                CancellationToken = default,
                MaxDegreeOfParallelism = _options.MaxParallelThreads,
            },
            async (instance, token) =>
            {
                try
                {
                    FileProperties fileProperties = await _updateInstanceService.UpdateInstanceBlobAsync(instance, datasetToUpdate, arguments.Partition, token);
                    updatedInstances.Add(
                        new InstanceMetadata(
                            instance.VersionedInstanceIdentifier,
                            new InstanceProperties
                            {
                                FileProperties = fileProperties,
                                NewVersion = instance.InstanceProperties.NewVersion,
                                OriginalVersion = instance.InstanceProperties.OriginalVersion
                            }));
                }
                catch (DataStoreRequestFailedException ex)
                {
                    logger.LogInformation("Failed to update instance with watermark {Watermark}, IsExternal {IsExternal}", instance.VersionedInstanceIdentifier.Version, ex.IsExternal);
                    errors.Add($"{ex.Message}. {ToInstanceString(instance.VersionedInstanceIdentifier)}");
                }
                catch (DataStoreException ex)
                {
                    logger.LogInformation("Failed to update instance with watermark {Watermark}, IsExternal {IsExternal}", instance.VersionedInstanceIdentifier.Version, ex.IsExternal);
                    errors.Add($"Failed to update instance. {ToInstanceString(instance.VersionedInstanceIdentifier)}");
                }
            });

        logger.LogInformation("Completed updating all instance blobs. Total instace count {TotalCount}. Total Failed {FailedCount}", arguments.InstanceMetadataList.Count, errors.Count);

        return new UpdateInstanceResponse(updatedInstances.ToList(), errors.ToList());
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
    [Obsolete("This function is obsolete. Use CompleteUpdateStudyV3Async instead.")]
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
                GetDeserializedDataset(arguments.ChangeDataset),
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
    /// Asynchronously commits all the instances in a study and creates new entries for changefeed.
    /// </summary>
    /// <param name="arguments">CompleteInstanceArguments</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="CompleteUpdateStudyV3Async"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(CompleteUpdateStudyV3Async))]
    public async Task CompleteUpdateStudyV3Async([ActivityTrigger] CompleteStudyArgumentsV2 arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.ChangeDataset, nameof(arguments.ChangeDataset));
        EnsureArg.IsNotNull(arguments.StudyInstanceUid, nameof(arguments.StudyInstanceUid));
        EnsureArg.IsNotNull(arguments.InstanceMetadataList, nameof(arguments.InstanceMetadataList));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Completing updating operation for study.");

        await _indexStore.EndUpdateInstanceAsync(
            arguments.PartitionKey,
            arguments.StudyInstanceUid,
            GetDeserializedDataset(arguments.ChangeDataset),
            arguments.InstanceMetadataList,
            CancellationToken.None);

        logger.LogInformation("Updating study completed successfully.");
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
                await _updateInstanceService.DeleteInstanceBlobAsync(instance.VersionedInstanceIdentifier.Version, partition, instance.InstanceProperties.FileProperties, token);
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
                await _updateInstanceService.DeleteInstanceBlobAsync(instance.InstanceProperties.NewVersion.Value, partition, instance.InstanceProperties.FileProperties, token);
            });

        logger.LogInformation("New blobs deleted successfully. Total size {TotalCount}", fileCount);
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
               await _fileStore.SetBlobToColdAccessTierAsync(instance.VersionedInstanceIdentifier.Version, partition, instance.InstanceProperties.FileProperties, token);
           });

        logger.LogInformation("Original version blob is moved to cold access tier successfully. Total size {TotalCount}", fileCount);
    }

    private static string ToInstanceString(VersionedInstanceIdentifier versionedInstanceIdentifier)
        => $"PartitionKey: {versionedInstanceIdentifier.Partition.Name}, StudyInstanceUID: {versionedInstanceIdentifier.StudyInstanceUid}, SeriesInstanceUID: {versionedInstanceIdentifier.SeriesInstanceUid}, SOPInstanceUID: {versionedInstanceIdentifier.SopInstanceUid}";

    private DicomDataset GetDeserializedDataset(string dataset) => JsonSerializer.Deserialize<DicomDataset>(dataset, _jsonSerializerOptions);
}
