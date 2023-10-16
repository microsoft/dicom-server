// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Functions.DataCleanup.Models;

namespace Microsoft.Health.Dicom.Functions.DataCleanup;

public partial class DataCleanupDurableFunction
{
    ///<summary>
    /// Asynchronously retrieves the next set of instance batches based on the configured options.
    /// </summary>
    /// <param name="arguments">The options for configuring the batches.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the asynchronous get operation. The value of its <see cref="Task{TResult}.Result"/>
    /// property contains a list of batches as defined by their smallest and largest watermark.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(GetInstanceBatchesByTimeStampAsync))]
    public Task<IReadOnlyList<WatermarkRange>> GetInstanceBatchesByTimeStampAsync(
        [ActivityTrigger] DataCleanupBatchCreationArguments arguments,
        ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(logger, nameof(logger));

        if (arguments.MaxWatermark.HasValue)
        {
            logger.LogInformation("Dividing up the instances into batches starting from the largest watermark {Watermark}.", arguments.MaxWatermark);
        }
        else
        {
            logger.LogInformation("Dividing up the instances into batches starting from the end.");
        }

        return _instanceStore.GetInstanceBatchesByTimeStampAsync(
            arguments.BatchSize,
            arguments.MaxParallelBatches,
            IndexStatus.Created,
            arguments.StartFilterTimeStamp,
            arguments.EndFilterTimeStamp,
            arguments.MaxWatermark,
            CancellationToken.None);
    }

    /// <summary>
    /// Asynchronously update HasFrameMetadata for the instances in the given watermark range.
    /// </summary>
    /// <param name="watermarkRange">The options that include the instances to clean up</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>A task representing the <see cref="CleanupFrameRangeDataAsync"/> operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(CleanupFrameRangeDataAsync))]
    public async Task CleanupFrameRangeDataAsync([ActivityTrigger] WatermarkRange watermarkRange, ILogger logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));

        IReadOnlyList<VersionedInstanceIdentifier> instanceIdentifiers =
            await _instanceStore.GetInstanceIdentifiersByWatermarkRangeAsync(watermarkRange, IndexStatus.Created);

        logger.LogInformation("Getting isFrameRangeExists for the instances in the range {Range}.", watermarkRange);

        var concurrentDictionary = new ConcurrentDictionary<VersionedInstanceIdentifier, bool>();
        await Parallel.ForEachAsync(
            instanceIdentifiers,
            new ParallelOptions
            {
                CancellationToken = default,
                MaxDegreeOfParallelism = _options.MaxParallelThreads,
            },
            async (instanceIdentifier, token) =>
            {
                bool isFrameRangeExists = await _metadataStore.DoesFrameRangeExistAsync(instanceIdentifier.Version, token);
                if (isFrameRangeExists)
                    concurrentDictionary.TryAdd(instanceIdentifier, isFrameRangeExists);
            });

        logger.LogInformation("Completed getting isFrameRangeExists for the instances in the range {Range}.", watermarkRange);

        var groupByPartitionList = concurrentDictionary.GroupBy(x => x.Key.Partition.Key).ToList();

        await Parallel.ForEachAsync(
            groupByPartitionList,
            new ParallelOptions
            {
                CancellationToken = default,
                MaxDegreeOfParallelism = _options.MaxParallelThreads,
            },
            (dict, token) =>
            {
                return new ValueTask(_indexDataStore.UpdateFrameDataAsync(dict.Key, dict.Select(x => x.Key.Version).ToList(), hasFrameMetadata: true, token));
            });

        logger.LogInformation("Completed updating hasFrameMetadata in the range {Range}, {TotalInstanceUpdated}.", watermarkRange, concurrentDictionary.Count);
    }
}
