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
using Microsoft.Health.Dicom.Functions.Duplicate.Models;
using Microsoft.Health.Dicom.Functions.Indexing.Models;
using Microsoft.Health.Dicom.Functions.Utils;

namespace Microsoft.Health.Dicom.Functions.Duplicate;
public partial class DuplicateDurableFunction
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
    [FunctionName(nameof(GetDuplicateInstanceBatchesAsync))]
    public Task<IReadOnlyList<WatermarkRange>> GetDuplicateInstanceBatchesAsync(
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
    /// Asynchronously re-indexes a range of data.
    /// </summary>
    /// <param name="arguments">The options that include the instances to re-index and the query tags.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>A task representing the <see cref="DuplicateBatchAsync"/> operation.</returns>
    [FunctionName(nameof(DuplicateBatchAsync))]
    public async Task DuplicateBatchAsync([ActivityTrigger] DuplicateBatchArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Beginning to duplicate instances in the range {Range}",
            arguments.WatermarkRange);

        IReadOnlyList<VersionedInstanceIdentifier> instanceIdentifiers =
            await _instanceStore.GetInstanceIdentifiersByWatermarkRangeAsync(arguments.WatermarkRange, IndexStatus.Created);

        await BatchUtils.ExecuteBatchAsync(instanceIdentifiers, arguments.ThreadCount, id => _instanceDuplicater.DuplicateInstanceAsync(id));
        logger.LogInformation("Completed duplicating instances in the range {Range}.", arguments.WatermarkRange);
    }

    /// <summary>
    /// Asynchronously completes the operation by removing the association between the tags and the operation.
    /// </summary>
    /// <param name="context">The context for the activity.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="CompleteDuplicateAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property contains the set of extended query tags
    /// whose re-indexing should be considered completed.
    /// </returns>
    [FunctionName(nameof(CompleteDuplicateAsync))]
    public Task CompleteDuplicateAsync(
        [ActivityTrigger] IDurableActivityContext context,
        ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Completing the duplicate operation {OperationId}", context.InstanceId);

        // TODO: update table storage to mark as completed.
        return Task.CompletedTask;
    }
}
