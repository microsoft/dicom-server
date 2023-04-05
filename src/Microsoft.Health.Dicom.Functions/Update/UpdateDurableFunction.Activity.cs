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
    /// <param name="context">The context for the activity.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="GetInstanceWatermarksInStudyAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property contains the subset of query tags
    /// that have been associated the operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(GetInstanceWatermarksInStudyAsync))]
    public async Task<IReadOnlyList<long>> GetInstanceWatermarksInStudyAsync(
        [ActivityTrigger] IDurableActivityContext context,
        ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Fetching all the instances in a study.");

        var inputArgument = context.GetInput<GetInstanceArguments>();

        var instanceMetadata = await _instanceStore.GetInstanceIdentifiersInStudyAsync(
            inputArgument.PartitionKey,
            inputArgument.StudyInstanceUid,
            cancellationToken: CancellationToken.None);

        return instanceMetadata.Select(x => x.Version).ToList();
    }

    /// <summary>
    /// Asynchronously completes the operation by removing the association between the tags and the operation.
    /// </summary>
    /// <param name="context">The context for the activity.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="CompleteUpdateInstanceAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property contains the set of extended query tags
    /// whose re-indexing should be considered completed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    //[FunctionName(nameof(CompleteUpdateInstanceAsync))]
    //public Task CompleteUpdateInstanceAsync(
    //    [ActivityTrigger] IDurableActivityContext context,
    //    ILogger logger)
    //{
    //    EnsureArg.IsNotNull(context, nameof(context));
    //    EnsureArg.IsNotNull(logger, nameof(logger));

    //    IReadOnlyList<int> tagKeys = context.GetInput<IReadOnlyList<int>>();
    //    logger.LogInformation("Completing the re-indexing operation {OperationId} for {Count} query tags {{{TagKeys}}}",
    //        context.InstanceId,
    //        tagKeys.Count,
    //        string.Join(", ", tagKeys));

    //    return _extendedQueryTagStore.CompleteReindexingAsync(tagKeys, CancellationToken.None);
    //}
}
