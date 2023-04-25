// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Model;
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

        UpdateCheckpoint input = context.GetInput<UpdateCheckpoint>();

        // Backfill batching options
        input.Batching ??= new BatchingOptions
        {
            MaxParallelCount = _options.MaxParallelBatches,
            Size = _options.BatchSize,
        };

        input.TotalNumberOfStudies ??= input.StudyInstanceUids.Count;

        if (input.StudyInstanceUids.Count > 0)
        {
            string studyInstanceUid = input.StudyInstanceUids[0];

            logger.LogInformation("Beginning to get all instances in a study.");

            IReadOnlyList<InstanceFileIdentifier> instanceWatermarks = await context.CallActivityWithRetryAsync<IReadOnlyList<InstanceFileIdentifier>>(
                nameof(GetInstanceWatermarksInStudyAsync),
                _options.RetryOptions,
                new GetInstanceArguments(input.PartitionKey, studyInstanceUid));

            logger.LogInformation("Getting all instances completed {TotalCount}", instanceWatermarks.Count);

            if (instanceWatermarks.Count > 0)
            {
                instanceWatermarks = await context.CallActivityWithRetryAsync<IReadOnlyList<InstanceFileIdentifier>>(
                        nameof(UpdateInstanceWatermarkAsync),
                        _options.RetryOptions,
                        new BatchUpdateArguments(input.PartitionKey, instanceWatermarks, input.ChangeDataset));
            }

            if (instanceWatermarks.Count > 0)
            {
                try
                {
                    await context.CallActivityWithRetryAsync(
                        nameof(UpdateInstanceBatchAsync),
                        _options.RetryOptions,
                        new BatchUpdateArguments(input.PartitionKey, instanceWatermarks, input.ChangeDataset));

                    await context.CallActivityWithRetryAsync(
                        nameof(CompleteUpdateInstanceAsync),
                        _options.RetryOptions,
                        new CompleteInstanceArguments(input.PartitionKey, studyInstanceUid, input.ChangeDataset));
                    _updateMeter.UpdatedInstances.Add(instanceWatermarks.Count);
                }
                catch (FunctionFailedException ex)
                {
                    // TODO: Need to call cleanup orchestration on failure after retries.
                    logger.LogError(ex, "Failed to update instances for study", ex);
                    var errors = new List<string>
                    {
                        $"Failed to update instances for study {studyInstanceUid}",
                    };

                    if (input.Errors != null)
                        errors.AddRange(errors);

                    input.Errors = errors;
                }
            }

            var studyUids = input.StudyInstanceUids.Where(x => !x.Equals(studyInstanceUid, StringComparison.OrdinalIgnoreCase)).ToList();

            if (studyUids.Any())
            {
                logger.LogInformation("Completed updating the instances for a study. {TotalInstanceUpdatedInaStudy}. Continuing with new execution...", instanceWatermarks.Count);
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
                    Batching = input.Batching,
                    StudyInstanceUids = studyUids,
                    ChangeDataset = input.ChangeDataset,
                    PartitionKey = input.PartitionKey,
                    TotalNumberOfStudies = input.TotalNumberOfStudies,
                    NumberOfStudyCompleted = input.NumberOfStudyCompleted + 1,
                    TotalNumberOfInstanceUpdated = input.TotalNumberOfInstanceUpdated + instanceWatermarks.Count,
                    Errors = input.Errors,
                    CreatedTime = input.CreatedTime ?? await context.GetCreatedTimeAsync(_options.RetryOptions),
                });
        }
        else
        {
            if (input.Errors != null && input.Errors.Count > 0)
            {
                logger.LogInformation("Update operation completed with errors.");

                // Throwing the exception so that it can set the operation status to Failed
                throw new OperationErrorException("Update operation completed with errors.");
            }
            else
            {
                logger.LogInformation("Update operation completed successfully");
            }
        }
    }
}
