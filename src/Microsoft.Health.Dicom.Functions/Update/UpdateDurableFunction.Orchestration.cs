// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.Registration;
using Microsoft.Health.Dicom.Functions.Update.Models;
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.Update;

public partial class UpdateDurableFunction
{
    /// <summary>
    /// Asynchronously updates list of instances in a study
    /// </summary>
    /// <remarks>
    /// Durable functions are reliable, and their implementations will be executed repeatedly over the lifetime of
    /// a single instance.
    /// </remarks>
    /// <param name="context">The context for the orchestration instance.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>A task representing the <see cref="UpdateInstancesAsync"/> operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="FormatException">Orchestration instance ID is invalid.</exception>
    [FunctionName(nameof(UpdateInstancesAsync))]
    public async Task UpdateInstancesAsync(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context)).ThrowIfInvalidOperationId();
        logger = context.CreateReplaySafeLogger(EnsureArg.IsNotNull(logger, nameof(logger)));
        ReplaySafeCounter<int> replaySafeCounter = context.CreateReplaySafeCounter(_updateMeter.UpdatedInstances);

        UpdateCheckpoint input = context.GetInput<UpdateCheckpoint>();

        if (input.NumberOfStudyCompleted < input.TotalNumberOfStudies)
        {
            string studyInstanceUid = input.StudyInstanceUids[input.NumberOfStudyCompleted];

            logger.LogInformation("Beginning to update all instances new watermark in a study.");

            IReadOnlyList<InstanceFileState> instanceWatermarks = await context.CallActivityWithRetryAsync<IReadOnlyList<InstanceFileState>>(
                nameof(UpdateInstanceWatermarkAsync),
                _options.RetryOptions,
                new UpdateInstanceWatermarkArguments(input.PartitionKey, studyInstanceUid));

            logger.LogInformation("Updated all instances new watermark in a study. Found {InstanceCount} instance for study", instanceWatermarks.Count);

            var totalNoOfInstances = input.TotalNumberOfInstanceUpdated;
            int numberofStudyFailed = input.NumberOfStudyFailed;

            if (instanceWatermarks.Count > 0)
            {
                try
                {
                    await context.CallActivityWithRetryAsync(
                        nameof(UpdateInstanceBlobsAsync),
                        _options.RetryOptions,
                        new UpdateInstanceBlobArguments(input.PartitionKey, instanceWatermarks, input.ChangeDataset));

                    await context.CallActivityWithRetryAsync(
                        nameof(CompleteUpdateStudyAsync),
                        _options.RetryOptions,
                        new CompleteStudyArguments(input.PartitionKey, studyInstanceUid, input.ChangeDataset));

                    totalNoOfInstances += instanceWatermarks.Count;
                }
                catch (FunctionFailedException ex)
                {
                    logger.LogError(ex, "Failed to update instances for study", ex);
                    var errors = new List<string>
                    {
                        $"Failed to update instances for study {studyInstanceUid}",
                    };

                    if (input.Errors != null)
                        errors.AddRange(errors);

                    input.Errors = errors;

                    numberofStudyFailed++;

                    // Cleanup the new version when the update activity fails
                    await TryCleanupActivity(context, instanceWatermarks);
                }
            }

            var numberOfStudyCompleted = input.NumberOfStudyCompleted + 1;

            if (input.TotalNumberOfStudies != numberOfStudyCompleted)
            {
                logger.LogInformation("Completed updating the instances for a study. {Updated}. Continuing with new execution...", instanceWatermarks.Count);
            }
            else
            {
                await context.CallActivityWithRetryAsync(
                    nameof(DeleteOldVersionBlobAsync),
                    _options.RetryOptions,
                    instanceWatermarks);
            }

            context.ContinueAsNew(
                new UpdateCheckpoint
                {
                    StudyInstanceUids = input.StudyInstanceUids,
                    ChangeDataset = input.ChangeDataset,
                    PartitionKey = input.PartitionKey,
                    NumberOfStudyCompleted = numberOfStudyCompleted,
                    NumberOfStudyFailed = numberofStudyFailed,
                    TotalNumberOfInstanceUpdated = totalNoOfInstances,
                    Errors = input.Errors,
                    CreatedTime = input.CreatedTime ?? await context.GetCreatedTimeAsync(_options.RetryOptions),
                });
        }
        else
        {
            if (input.Errors?.Count > 0)
            {
                logger.LogWarning("Update operation completed with errors. {NumberOfStudyUpdated}, {NumberOfStudyFailed}, {TotalNumberOfInstanceUpdated}.",
                    input.NumberOfStudyCompleted - input.NumberOfStudyFailed,
                    input.NumberOfStudyFailed,
                    input.TotalNumberOfInstanceUpdated);

                // Throwing the exception so that it can set the operation status to Failed
                throw new OperationErrorException("Update operation completed with errors.");
            }
            else
            {
                logger.LogInformation("Update operation completed successfully. {NumberOfStudyUpdated}, {TotalNumberOfInstanceUpdated}.",
                    input.NumberOfStudyCompleted,
                    input.TotalNumberOfInstanceUpdated);
            }

            if (input.TotalNumberOfInstanceUpdated > 0)
            {
                replaySafeCounter.Add(input.TotalNumberOfInstanceUpdated);
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Using a generic exception to catch all scenarios.")]
    private async Task TryCleanupActivity(IDurableOrchestrationContext context, IReadOnlyList<InstanceFileState> instanceWatermarks)
    {
        try
        {
            await context.CallActivityWithRetryAsync(
                nameof(CleanupNewVersionBlobAsync),
                _options.RetryOptions,
                instanceWatermarks);
        }
        catch (Exception) { }
    }
}
