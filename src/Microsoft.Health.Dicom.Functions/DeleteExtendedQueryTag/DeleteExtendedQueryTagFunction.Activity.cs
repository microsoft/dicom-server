// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag.Models;

namespace Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag;

public partial class DeleteExtendedQueryTagFunction
{
    [FunctionName(nameof(GetExtendedQueryTagAsync))]
    public Task<ExtendedQueryTagStoreJoinEntry> GetExtendedQueryTagAsync(
        [ActivityTrigger] string tagPath,
        ILogger logger)
    {
        EnsureArg.IsNotNull(tagPath, nameof(tagPath));
        EnsureArg.IsNotNull(logger, nameof(logger));

        return _extendedQueryTagStore.GetExtendedQueryTagAsync(tagPath);
    }

    [FunctionName(nameof(UpdateExtendedQueryTagStatusToDeleting))]
    public Task UpdateExtendedQueryTagStatusToDeleting(
    [ActivityTrigger] int tagKey,
    ILogger logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Updating the status of tag {TagKey} to Deleting", tagKey);

        return _extendedQueryTagStore.UpdateExtendedQueryTagStatusToDelete(tagKey);
    }

    [FunctionName(nameof(GetExtendedQueryTagBatchesAsync))]
    public Task<IReadOnlyList<WatermarkRange>> GetExtendedQueryTagBatchesAsync([ActivityTrigger] BatchCreationArguments input, ILogger logger)
    {
        EnsureArg.IsNotNull(input, nameof(input));
        EnsureArg.IsNotNull(logger, nameof(logger));

        return _extendedQueryTagStore.GetExtendedQueryTagBatches(input.BatchSize, input.BatchCount, input.VR, input.TagKey);
    }

    [FunctionName(nameof(DeleteExtendedQueryTagDataByWatermarkRangeAsync))]
    public Task DeleteExtendedQueryTagDataByWatermarkRangeAsync(
        [ActivityTrigger] DeleteBatchArguments deleteBatchArguments,
        ILogger logger)
    {
        EnsureArg.IsNotNull(deleteBatchArguments, nameof(deleteBatchArguments));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Deleting batch of tag {TagKey} in range {Range}", deleteBatchArguments.TagKey, deleteBatchArguments.Range);

        return _extendedQueryTagStore.DeleteExtendedQueryTagDataByWatermarkRangeAsync(deleteBatchArguments.Range.Start, deleteBatchArguments.Range.End, deleteBatchArguments.VR, deleteBatchArguments.TagKey);
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
