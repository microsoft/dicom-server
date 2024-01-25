// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag.Models;

namespace Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag;

public partial class DeleteExtendedQueryTagFunction
{
    [FunctionName(nameof(GetExtendedQueryTagAsync))]
    public Task<ExtendedQueryTagStoreJoinEntry> GetExtendedQueryTagAsync([ActivityTrigger] IDurableOrchestrationContext context, ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsNotNull(logger, nameof(logger));

        // TODO: is this the right way to get input?
        string tagPath = context.GetInput<string>();

        return _extendedQueryTagStore.GetExtendedQueryTagAsync(tagPath);
    }

    [FunctionName(nameof(UpdateTagStatusToDeletingAsync))]
    public Task UpdateTagStatusToDeletingAsync(
        [ActivityTrigger] DeleteExtendedQueryTagArguments deleteExtendedQueryTagArguments,
        ILogger logger)
    {
        EnsureArg.IsNotNull(deleteExtendedQueryTagArguments, nameof(deleteExtendedQueryTagArguments));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Updating the status of tag {TagPath} to Deleting");

        return _extendedQueryTagStore.UpdateExtendedQueryTagStatusAsync(deleteExtendedQueryTagArguments.TagKey, ExtendedQueryTagStatus.Deleting);
    }

    [FunctionName(nameof(DeleteExtendedQueryTagIndexBatchAsync))]
    public Task<int> DeleteExtendedQueryTagIndexBatchAsync(
        [ActivityTrigger] DeleteExtendedQueryTagArguments deleteExtendedQueryTagArguments,
        ILogger logger)
    {
        EnsureArg.IsNotNull(deleteExtendedQueryTagArguments, nameof(deleteExtendedQueryTagArguments));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Deleting batch of tag {TagPath}", deleteExtendedQueryTagArguments.TagKey);

        return _extendedQueryTagStore.DeleteExtendedQueryTagIndexBatchAsync(deleteExtendedQueryTagArguments.TagKey, deleteExtendedQueryTagArguments.VR, _options.BatchSize);
    }

    [FunctionName(nameof(DeleteExtendedQueryTagErrorBatchAsync))]
    public Task<int> DeleteExtendedQueryTagErrorBatchAsync(
        [ActivityTrigger] DeleteExtendedQueryTagArguments deleteExtendedQueryTagArguments,
        ILogger logger)
    {
        EnsureArg.IsNotNull(deleteExtendedQueryTagArguments, nameof(deleteExtendedQueryTagArguments));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Deleting extended query tag errors for tag path {TagPath}", deleteExtendedQueryTagArguments.TagKey);

        return _extendedQueryTagErrorStore.DeleteExtendedQueryTagErrorBatch(deleteExtendedQueryTagArguments.TagKey, _options.BatchSize);
    }

    [FunctionName(nameof(DeleteExtendedQueryTagEntry))]
    public Task DeleteExtendedQueryTagEntry(
        [ActivityTrigger] DeleteExtendedQueryTagArguments deleteExtendedQueryTagArguments,
        ILogger logger)
    {
        EnsureArg.IsNotNull(deleteExtendedQueryTagArguments, nameof(deleteExtendedQueryTagArguments));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Deleting extended query tag {TagPath}", deleteExtendedQueryTagArguments.TagKey);

        return _extendedQueryTagStore.DeleteExtendedQueryTagEntryAsync(deleteExtendedQueryTagArguments.TagKey);
    }
}
