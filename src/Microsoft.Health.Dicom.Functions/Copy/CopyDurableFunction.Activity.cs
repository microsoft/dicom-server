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
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Functions.Indexing.Models;

namespace Microsoft.Health.Dicom.Functions.Copy;

public partial class CopyDurableFunction
{
    /// <summary>
    /// Asynchronously retrieves the next set of instance batches based on the configured options.
    /// </summary>
    /// <param name="arguments">The options for configuring the batches.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the asynchronous get operation. The value of its <see cref="Task{TResult}.Result"/>
    /// property contains a list of batches as defined by their smallest and largest watermark.
    /// </returns>
    [FunctionName(nameof(GetCopyInstanceBatchesAsync))]
    public Task<IReadOnlyList<WatermarkRange>> GetCopyInstanceBatchesAsync(
        [ActivityTrigger] BatchCreationArguments arguments,
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

        return _instanceStore.GetInstanceBatchesAsync(
            arguments.BatchSize,
            arguments.MaxParallelBatches,
            IndexStatus.Created,
            arguments.MaxWatermark,
            CancellationToken.None);
    }

    /// <summary>
    /// Asynchronously copy a range of DICOM instances.
    /// </summary>
    /// <param name="range">The options that include the instances to copy.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>A task representing the <see cref="CopyBatchAsync"/> operation.</returns>
    [FunctionName(nameof(CopyBatchAsync))]
    public async Task CopyBatchAsync([ActivityTrigger] WatermarkRange range, ILogger logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Beginning to copy instances in the range {Range}", range);

        IReadOnlyList<VersionedInstanceIdentifier> instanceIdentifiers =
            await _instanceStore.GetInstanceIdentifiersByWatermarkRangeAsync(range, IndexStatus.Created);

        await Parallel.ForEachAsync(
            instanceIdentifiers,
            new ParallelOptions
            {
                CancellationToken = default,
                MaxDegreeOfParallelism = _options.MaxParallelThreads,
            },
            (id, token) => new ValueTask(_instanceCopier.CopyInstanceAsync(id, token)));
        logger.LogInformation("Completed copying instances in the range {Range}.", range);
    }
}
