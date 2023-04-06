// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Functions.Update.Models;

namespace Microsoft.Health.Dicom.Functions.Update;

public partial class UpdateDurableFunction
{
    /// <summary>
    /// Asynchronously retrieves the query tags that have been associated with the operation.
    /// </summary>
    /// <param name="arguments">Get instance argument.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="GetInstanceWatermarksInStudyAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property contains the subset of query tags
    /// that have been associated the operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(GetInstanceWatermarksInStudyAsync))]
    public async Task<IReadOnlyList<long>> GetInstanceWatermarksInStudyAsync(
        [ActivityTrigger] GetInstanceArguments arguments,
        ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Fetching all the instances in a study.");

        var instanceMetadata = await _instanceStore.GetInstanceIdentifiersInStudyAsync(
            arguments.PartitionKey,
            arguments.StudyInstanceUid,
            cancellationToken: CancellationToken.None);

        return instanceMetadata.Select(x => x.Version).ToList();
    }

    [FunctionName(nameof(UpdateInstanceBatchAsync))]
    public async Task UpdateInstanceBatchAsync([ActivityTrigger] BatchUpdateArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.InstanceWatermarks, nameof(arguments.InstanceWatermarks));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("");

        int processed = 0;

        logger.LogInformation("Beginning to update all instance blobs, Total count {TotalCount}", arguments.InstanceWatermarks.Count);

        while (processed < arguments.InstanceWatermarks.Count)
        {
            int batchSize = Math.Min(arguments.BatchSize, arguments.InstanceWatermarks.Count - processed);
            var batch = arguments.InstanceWatermarks.Skip(processed).Take(batchSize).ToList();

            if (batch.Count > 0)
            {
                logger.LogInformation("Beginning to update instance watermark with {StartingRange} and {EndingRange}. Total batchSize {BatchSize}.",
                    batch[0],
                    batch[^1],
                    batchSize);

                await _indexStore.BeginUpdateInstanceAsync(arguments.PartitionKey, batch);

                logger.LogInformation("Completed updating instance with {StartingRange} and {EndingRange}. Total batchSize {BatchSize}.",
                    batch[0],
                    batch[^1],
                    batchSize);

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
                    async (watermark, token) =>
                    {
                        // TODO: Copy and update DICOM file
                        // TODO: Copy and update metadata file
                        await Task.CompletedTask;
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

    [FunctionName(nameof(CompleteUpdateInstanceAsync))]
    public Task CompleteUpdateInstanceAsync([ActivityTrigger] CompleteInstanceArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Completing updating operation for study.");

        return _indexStore.EndUpdateInstanceAsync(_options.BatchSize, arguments.PartitionKey, arguments.StudyInstanceUid, arguments.Dataset, CancellationToken.None);
    }
}
