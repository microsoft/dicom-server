// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.BlobMigration.Models;

namespace Microsoft.Health.Dicom.Functions.BlobMigration;

public partial class CleanupDeletedDurableFunction
{
    /// <summary>
    /// Asynchronously retrieves the next set of instance batches based on the configured options.
    /// </summary>
    /// <param name="arguments">The options for configuring the batches.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the asynchronous get operation. The value of its <see cref="Task{TResult}.Result"/>
    /// </returns>
    [FunctionName(nameof(GetDeletedChangeFeedInstanceBatchesAsync))]
    public async Task<IReadOnlyCollection<ChangeFeedEntry>> GetDeletedChangeFeedInstanceBatchesAsync(
        [ActivityTrigger] CleanupDeletedBatchArguments arguments,
        ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(logger, nameof(logger));

        return await _changeFeedStore.GetDeletedChangeFeedByWatermarkOrTimeStampAsync(
            arguments.BatchSize,
            arguments.FilterTimeStamp,
            arguments.BatchRange,
            CancellationToken.None);
    }

    /// <summary>
    /// Retrieves max watermark in all changefeed deleted entries.
    /// </summary>
    /// <param name="arguments">The options for configuring the batches.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the asynchronous get operation. The value of its <see cref="Task{TResult}.Result"/>
    /// </returns>
    [FunctionName(nameof(GetMaxDeletedChangeFeedWatermarkAsync))]
    public Task<long> GetMaxDeletedChangeFeedWatermarkAsync(
        [ActivityTrigger] CleanupDeletedBatchArguments arguments,
        ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsTrue(arguments.FilterTimeStamp.HasValue, nameof(arguments.FilterTimeStamp));

        return _changeFeedStore.GetMaxDeletedChangeFeedWatermarkAsync(arguments.FilterTimeStamp.Value, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronously deletes a range of DICOM deleted files.
    /// </summary>
    /// <param name="instanceIdentifiers">The instances to delete.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>A task representing the <see cref="CleanupDeletedBatchAsync"/> operation.</returns>
    [FunctionName(nameof(CleanupDeletedBatchAsync))]
    public async Task CleanupDeletedBatchAsync([ActivityTrigger] IReadOnlyCollection<VersionedInstanceIdentifier> instanceIdentifiers, ILogger logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Beginning to delete old format instances");

        await Parallel.ForEachAsync(
            instanceIdentifiers,
            new ParallelOptions
            {
                CancellationToken = default,
                MaxDegreeOfParallelism = _options.MaxParallelThreads,
            },
            (id, token) => new ValueTask(_blobMigrationService.DeleteInstanceAsync(id, forceDelete: true, token)));

        logger.LogInformation("Completed deleting old format instances");
    }
}
