// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
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
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.Update.Models;

namespace Microsoft.Health.Dicom.Functions.Update;

public partial class UpdateDurableFunction
{
    /// <summary>
    /// Asynchronously retrieves list of instances watermarks that matches the study uid.
    /// </summary>
    /// <param name="arguments">Get instance watermarks argument.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="GetInstanceWatermarksInStudyAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property contains list of watermarks and original watermarks
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(GetInstanceWatermarksInStudyAsync))]
    public async Task<IReadOnlyList<InstanceFileIdentifier>> GetInstanceWatermarksInStudyAsync(
        [ActivityTrigger] GetInstanceArguments arguments,
        ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Fetching all the instances in a study.");

        IEnumerable<InstanceMetadata> instanceMetadata = await _instanceStore.GetInstanceIdentifierWithPropertiesAsync(
            arguments.PartitionKey,
            arguments.StudyInstanceUid,
            cancellationToken: CancellationToken.None);

        return instanceMetadata.Select(x =>
            new InstanceFileIdentifier
            {
                Version = x.VersionedInstanceIdentifier.Version,
                OriginalVersion = x.InstanceProperties.OriginalVersion,
                NewVersion = x.InstanceProperties.NewVersion
            }).ToList();
    }

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
    public async Task<IReadOnlyList<InstanceFileIdentifier>> UpdateInstanceWatermarkAsync([ActivityTrigger] BatchUpdateArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.InstanceWatermarks, nameof(arguments.InstanceWatermarks));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Beginning to update all instance watermarks, Total count {TotalCount}", arguments.InstanceWatermarks.Count);

        var instanceWatermarks = arguments.InstanceWatermarks.Select(x => x.Version).ToList();

        IEnumerable<InstanceMetadata> instanceMetadata = await _indexStore.BeginUpdateInstanceAsync(arguments.PartitionKey, instanceWatermarks, CancellationToken.None);

        logger.LogInformation("Completed updating all instance watermarks, Total count {TotalCount}", arguments.InstanceWatermarks.Count);

        return instanceMetadata.Select(x =>
            new InstanceFileIdentifier
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
    /// A task representing the <see cref="UpdateInstanceBatchAsync"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(UpdateInstanceBatchAsync))]
    public async Task UpdateInstanceBatchAsync([ActivityTrigger] BatchUpdateArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.ChangeDataset, nameof(arguments.ChangeDataset));
        EnsureArg.IsNotNull(arguments.InstanceWatermarks, nameof(arguments.InstanceWatermarks));
        EnsureArg.IsNotNull(logger, nameof(logger));

        DicomDataset datasetToUpdate = GetDeserialzedDataset(arguments.ChangeDataset);

        int processed = 0;

        logger.LogInformation("Beginning to update all instance blobs, Total count {TotalCount}", arguments.InstanceWatermarks.Count);

        while (processed < arguments.InstanceWatermarks.Count)
        {
            int batchSize = Math.Min(_options.BatchSize, arguments.InstanceWatermarks.Count - processed);
            var batch = arguments.InstanceWatermarks.Skip(processed).Take(batchSize).ToList();

            if (batch.Count > 0)
            {
                logger.LogInformation("Beginning to update instance blobs starting with {StartingRange} and {EndingRange}. Total batchSize {BatchSize}.",
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
                        await _updateInstanceService.UpdateInstanceBlobAsync(instance, datasetToUpdate, token);
                        _updateMeter.UpdatedInstances.Add(1);
                    });

                logger.LogInformation("Completed updating instance blobs starting with {StartingRange} and {EndingRange}. Total batchSize {BatchSize}.",
                    batch[0],
                    batch[^1],
                    batchSize);

                processed += batchSize;
            }
        }

        logger.LogInformation("Completed updating all instance blobs");
    }

    /// <summary>
    /// Asynchronously commits all the instances in a study and creates new entries for changefeed.
    /// </summary>
    /// <param name="arguments">CompleteInstanceArguments</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="CompleteUpdateInstanceAsync"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(CompleteUpdateInstanceAsync))]
    public async Task CompleteUpdateInstanceAsync([ActivityTrigger] CompleteInstanceArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.ChangeDataset, nameof(arguments.ChangeDataset));
        EnsureArg.IsNotNull(arguments.StudyInstanceUid, nameof(arguments.StudyInstanceUid));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Completing updating operation for study.");

        try
        {
            await _indexStore.EndUpdateInstanceAsync(
                arguments.PartitionKey,
                arguments.StudyInstanceUid,
                GetDeserialzedDataset(arguments.ChangeDataset),
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
        EnsureArg.IsNotNull(logger, nameof(logger));

        IReadOnlyList<InstanceFileIdentifier> fileIdentifiers = context.GetInput<IReadOnlyList<InstanceFileIdentifier>>();

        logger.LogInformation("Begin deleting old blobs. Total size {TotalCount}", fileIdentifiers.Count);

        await Parallel.ForEachAsync(
            fileIdentifiers,
            new ParallelOptions
            {
                CancellationToken = default,
                MaxDegreeOfParallelism = _options.MaxParallelThreads,
            },
            async (fileIdentifier, token) =>
            {
                if (fileIdentifier.OriginalVersion.HasValue)
                {
                    await _updateInstanceService.DeleteInstanceBlobAsync(fileIdentifier.Version, token);
                }
            });

        logger.LogInformation("Old blobs deleted successfully. Total size {TotalCount}", fileIdentifiers.Count);
    }

    private DicomDataset GetDeserialzedDataset(string dataset) => JsonSerializer.Deserialize<DicomDataset>(dataset, _jsonSerializerOptions);
}
