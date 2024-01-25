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
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag;

public partial class DeleteExtendedQueryTagFunction
{
    [FunctionName(nameof(DeleteExtendedQueryTagAsync))]
    public async Task DeleteExtendedQueryTagAsync(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context)).ThrowIfInvalidOperationId();
        logger = context.CreateReplaySafeLogger(EnsureArg.IsNotNull(logger, nameof(logger)));

        // TODO: is this the right way to get input?
        string tagPath = context.GetInput<string>();

        // get query tag
        ExtendedQueryTagStoreJoinEntry queryTag = await context.CallActivityWithRetryAsync<ExtendedQueryTagStoreJoinEntry>(nameof(GetExtendedQueryTagAsync), _options.RetryOptions, tagPath);

        DeleteExtendedQueryTagArguments arguments = new DeleteExtendedQueryTagArguments()
        {
            TagKey = queryTag.Key,
            VR = queryTag.VR,
        };

        // update tag status to deleting
        await context.CallActivityWithRetryAsync(nameof(UpdateTagStatusToDeletingAsync), _options.RetryOptions, arguments);

        // delete index data in batches
        int deletedRows = _options.BatchSize;
        while (deletedRows == _options.BatchSize)
        {
            deletedRows = await context.CallActivityWithRetryAsync<int>(nameof(DeleteExtendedQueryTagIndexBatchAsync), _options.RetryOptions, arguments);
        }

        // delete error data in batches
        deletedRows = _options.BatchSize;
        while (deletedRows == _options.BatchSize)
        {
            deletedRows = await context.CallActivityWithRetryAsync<int>(nameof(DeleteExtendedQueryTagErrorBatchAsync), _options.RetryOptions, arguments);
        }

        // delete the tag itself
        await context.CallActivityWithRetryAsync(nameof(DeleteExtendedQueryTagEntry), _options.RetryOptions, arguments);
    }
}
