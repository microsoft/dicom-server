// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Models.Update;
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.Update;

public partial class UpdateDurableFunction
{
    [FunctionName(nameof(UpdateInstancesAsync))]
    public async Task UpdateInstancesAsync(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context)).ThrowIfInvalidOperationId();
        logger = context.CreateReplaySafeLogger(EnsureArg.IsNotNull(logger, nameof(logger)));

        UpdateCheckpoint input = context.GetInput<UpdateCheckpoint>();

        await context.CallActivityWithRetryAsync(
                nameof(UpdateInstanceBlobAsync),
                _options.RetryOptions,
                new UpdateInstanceArgument
                {
                    spec = input.UpdateSpec,
                    Dataset = input.Dataset
                });
    }
}

public class UpdateInstanceArgument
{
    public UpdateSpecification spec { get; set; }
    public string Dataset { get; set; }
}
