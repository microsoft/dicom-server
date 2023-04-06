// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Functions.Update.Models;
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.Update;

public partial class UpdateDurableFunction
{
    /// <summary>
    /// Asynchronously creates an index for the provided query tags over the previously added data.
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

        if (input.StudyInstanceUids.Count > 0)
        {
            string studyInstanceUid = input.StudyInstanceUids[0];

            IReadOnlyList<long> instanceWatermarks = await context.CallActivityWithRetryAsync<IReadOnlyList<long>>(
                nameof(GetInstanceWatermarksInStudyAsync),
                _options.RetryOptions,
                new GetInstanceArguments(input.PartitionKey, studyInstanceUid));

            if (instanceWatermarks.Count > 0)
            {
                await context.CallActivityWithRetryAsync(
                    nameof(UpdateInstanceBatchAsync),
                    _options.RetryOptions,
                    new BatchUpdateArguments(input.PartitionKey, instanceWatermarks, _options.BatchSize));


                await context.CallActivityWithRetryAsync(
                    nameof(CompleteUpdateInstanceAsync),
                    _options.RetryOptions,
                    new CompleteInstanceArguments(input.PartitionKey, studyInstanceUid, input.ChangeDataset as DicomDataset));
            }

            var studyUids = input.StudyInstanceUids.Skip(1).ToList();

            if (studyUids.Any())
            {
                logger.LogInformation("Completed updating the instances for a study. Continuing with new execution...");

                context.ContinueAsNew(
                        new UpdateCheckpoint
                        {
                            Batching = input.Batching,
                            StudyInstanceUids = studyUids,
                            ChangeDataset = input.ChangeDataset,
                            PartitionKey = input.PartitionKey,
                            TotalNumberOfStudies = input.TotalNumberOfStudies,
                            NumberOfStudyCompleted = input.NumberOfStudyCompleted + 1,
                            CreatedTime = input.CreatedTime ?? await context.GetCreatedTimeAsync(_options.RetryOptions),
                        });
            }
            else
            {
                logger.LogInformation("Update operation completed successfully");
            }
        }
        else
        {
            logger.LogInformation("Update operation completed successfully");
        }
    }
}
