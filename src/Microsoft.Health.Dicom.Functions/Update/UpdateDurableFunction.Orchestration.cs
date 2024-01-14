// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.Diagnostic;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
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
    /// <returns>A task representing the <see cref="UpdateInstancesV5Async"/> operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="FormatException">Orchestration instance ID is invalid.</exception>
    [FunctionName(nameof(UpdateInstancesV5Async))]
    [Obsolete("This function is obsolete. Use UpdateInstancesV6Async instead.")]
    public async Task UpdateInstancesV5Async(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context)).ThrowIfInvalidOperationId();
        logger = context.CreateReplaySafeLogger(EnsureArg.IsNotNull(logger, nameof(logger)));
        ReplaySafeCounter<int> replaySafeCounter = context.CreateReplaySafeCounter(_updateMeter.UpdatedInstances);
        IReadOnlyList<InstanceMetadata> instanceMetadataList;
        UpdateCheckpoint input = context.GetInput<UpdateCheckpoint>();
        input.Partition ??= new Partition(input.PartitionKey, Partition.UnknownName);

        _auditLogger.LogAudit(
            AuditAction.Executing,
            AuditEventSubType.UpdateStudyOperation,
            null,
            null,
            Activity.Current?.RootId,
            null,
            null,
            null);

        if (input.NumberOfStudyCompleted < input.TotalNumberOfStudies)
        {
            string studyInstanceUid = input.StudyInstanceUids[input.NumberOfStudyCompleted];

            logger.LogInformation("Beginning to update all instances new watermark in a study.");

            IReadOnlyList<InstanceMetadata> instances = await context
                .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                    nameof(UpdateInstanceWatermarkV2Async),
                    _options.RetryOptions,
                    new UpdateInstanceWatermarkArgumentsV2(input.Partition, studyInstanceUid));
            var instanceWatermarks = instances.Select(x => x.ToInstanceFileState()).ToList();

            logger.LogInformation("Updated all instances new watermark in a study. Found {InstanceCount} instance for study", instances.Count);

            var totalNoOfInstances = input.TotalNumberOfInstanceUpdated;

            if (instances.Count > 0)
            {
                bool isFailedToUpdateStudy = false;

                try
                {
                    UpdateInstanceResponse response = await context.CallActivityWithRetryAsync<UpdateInstanceResponse>(
                        nameof(UpdateInstanceBlobsV3Async),
                        _options.RetryOptions,
                        new UpdateInstanceBlobArgumentsV2(input.Partition, instances, input.ChangeDataset));

                    instanceMetadataList = response.InstanceMetadataList;

                    if (response.Errors?.Count > 0)
                    {
                        isFailedToUpdateStudy = true;
                        logger.LogWarning("Failed to update instances for study. Total instance failed for study {TotalFailed}", response.Errors.Count);
                        await HandleException(context, input, studyInstanceUid, instances, response.Errors);
                    }
                    else
                    {
                        await context.CallActivityWithRetryAsync(
                            nameof(CompleteUpdateStudyV4Async),
                            _options.RetryOptions,
                            new CompleteStudyArgumentsV2(input.Partition.Key, studyInstanceUid, input.ChangeDataset, GetInstanceMetadataList(instanceMetadataList)));

                        totalNoOfInstances += instances.Count;
                    }
                }
                catch (FunctionFailedException ex)
                {
                    isFailedToUpdateStudy = true;

                    logger.LogError(ex, "Failed to update instances for study", ex);

                    await HandleException(context, input, studyInstanceUid, instances, null);
                }

                if (!isFailedToUpdateStudy)
                {
                    await context.CallActivityWithRetryAsync(
                        nameof(DeleteOldVersionBlobV3Async),
                        _options.RetryOptions,
                        new CleanupBlobArgumentsV2(instances, input.Partition));

                    await context.CallActivityWithRetryAsync(
                        nameof(SetOriginalBlobToColdAccessTierV2Async),
                        _options.RetryOptions,
                        new CleanupBlobArgumentsV2(instances, input.Partition));
                }
            }

            var numberOfStudyCompleted = input.NumberOfStudyCompleted + 1;

            if (input.TotalNumberOfStudies != numberOfStudyCompleted)
            {
                logger.LogInformation("Completed updating the instances for a study. {Updated}. Continuing with new execution...", instances.Count);
            }

            context.ContinueAsNew(
                new UpdateCheckpoint
                {
                    StudyInstanceUids = input.StudyInstanceUids,
                    ChangeDataset = input.ChangeDataset,
                    Partition = input.Partition,
                    PartitionKey = input.PartitionKey,
                    NumberOfStudyCompleted = numberOfStudyCompleted,
                    NumberOfStudyFailed = input.NumberOfStudyFailed,
                    TotalNumberOfInstanceUpdated = totalNoOfInstances,
                    Errors = input.Errors,
                    CreatedTime = input.CreatedTime ?? await context.GetCreatedTimeAsync(_options.RetryOptions),
                });
        }
        else
        {
            if (input.TotalNumberOfInstanceUpdated > 0)
            {
                replaySafeCounter.Add(input.TotalNumberOfInstanceUpdated);
            }

            string serializedInput = GetSerializedCheckpointResult(input);

            if (input.Errors?.Count > 0)
            {
                logger.LogWarning("Update operation completed with errors. {NumberOfStudyUpdated}, {NumberOfStudyFailed}, {TotalNumberOfInstanceUpdated}.",
                     input.NumberOfStudyCompleted - input.NumberOfStudyFailed,
                     input.NumberOfStudyFailed,
                     input.TotalNumberOfInstanceUpdated);

                _telemetryClient.ForwardOperationLogTrace(
                    "Update operation completed with errors",
                    context.InstanceId,
                    serializedInput,
                    AuditEventSubType.UpdateStudyOperation,
                    ApplicationInsights.DataContracts.SeverityLevel.Error);

                _auditLogger.LogAudit(
                    AuditAction.Executed,
                    AuditEventSubType.UpdateStudyOperation,
                    null,
                    HttpStatusCode.BadRequest,
                    Activity.Current?.RootId,
                    null,
                    null,
                    null);

                // Throwing the exception so that it can set the operation status to Failed
                throw new OperationErrorException("Update operation completed with errors.");
            }
            else
            {
                logger.LogInformation("Update operation completed successfully. {NumberOfStudyUpdated}, {TotalNumberOfInstanceUpdated}.",
                     input.NumberOfStudyCompleted,
                     input.TotalNumberOfInstanceUpdated);

                _telemetryClient.ForwardOperationLogTrace("Update operation completed successfully", context.InstanceId, serializedInput, AuditEventSubType.UpdateStudyOperation);

                _auditLogger.LogAudit(
                    AuditAction.Executed,
                    AuditEventSubType.UpdateStudyOperation,
                    null,
                    HttpStatusCode.OK,
                    Activity.Current?.RootId,
                    null,
                    null,
                    null);
            }
        }
    }

    /// <summary>
    /// Asynchronously updates list of instances in a study
    /// </summary>
    /// <remarks>
    /// Durable functions are reliable, and their implementations will be executed repeatedly over the lifetime of
    /// a single instance.
    /// </remarks>
    /// <param name="context">The context for the orchestration instance.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>A task representing the <see cref="UpdateInstancesV6Async"/> operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="FormatException">Orchestration instance ID is invalid.</exception>
    [FunctionName(nameof(UpdateInstancesV6Async))]
    public async Task UpdateInstancesV6Async(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context)).ThrowIfInvalidOperationId();
        logger = context.CreateReplaySafeLogger(EnsureArg.IsNotNull(logger, nameof(logger)));
        ReplaySafeCounter<int> replaySafeCounter = context.CreateReplaySafeCounter(_updateMeter.UpdatedInstances);
        IReadOnlyList<InstanceMetadata> instanceMetadataList;
        UpdateCheckpoint input = context.GetInput<UpdateCheckpoint>();
        input.Partition ??= new Partition(input.PartitionKey, Partition.UnknownName);

        _auditLogger.LogAudit(
            AuditAction.Executing,
            AuditEventSubType.UpdateStudyOperation,
            null,
            null,
            Activity.Current?.RootId,
            null,
            null,
            null);

        if (input.NumberOfStudyProcessed < input.TotalNumberOfStudies)
        {
            string studyInstanceUid = input.StudyInstanceUids[input.NumberOfStudyProcessed];

            logger.LogInformation("Beginning to update all instances new watermark in a study.");

            IReadOnlyList<InstanceMetadata> instances = await context
                .CallActivityWithRetryAsync<IReadOnlyList<InstanceMetadata>>(
                    nameof(UpdateInstanceWatermarkV2Async),
                    _options.RetryOptions,
                    new UpdateInstanceWatermarkArgumentsV2(input.Partition, studyInstanceUid));
            var instanceWatermarks = instances.Select(x => x.ToInstanceFileState()).ToList();

            logger.LogInformation("Updated all instances new watermark in a study. Found {InstanceCount} instance for study", instances.Count);

            int totalNoOfInstances = input.TotalNumberOfInstanceUpdated;
            int numberStudyUpdated = input.NumberOfStudyCompleted;

            if (instances.Count > 0)
            {
                bool isFailedToUpdateStudy = false;

                try
                {
                    UpdateInstanceResponse response = await context.CallActivityWithRetryAsync<UpdateInstanceResponse>(
                        nameof(UpdateInstanceBlobsV3Async),
                        _options.RetryOptions,
                        new UpdateInstanceBlobArgumentsV2(input.Partition, instances, input.ChangeDataset));

                    instanceMetadataList = response.InstanceMetadataList;

                    if (response.Errors?.Count > 0)
                    {
                        isFailedToUpdateStudy = true;
                        logger.LogWarning("Failed to update instances for study. Total instance failed for study {TotalFailed}", response.Errors.Count);
                        await HandleException(context, input, studyInstanceUid, instances, response.Errors);
                    }
                    else
                    {
                        await context.CallActivityWithRetryAsync(
                            nameof(CompleteUpdateStudyV4Async),
                            _options.RetryOptions,
                            new CompleteStudyArgumentsV2(input.Partition.Key, studyInstanceUid, input.ChangeDataset, GetInstanceMetadataList(instanceMetadataList)));

                        totalNoOfInstances += instances.Count;
                        numberStudyUpdated++;
                    }
                }
                catch (FunctionFailedException ex)
                {
                    isFailedToUpdateStudy = true;

                    logger.LogError(ex, "Failed to update instances for study", ex);

                    await HandleException(context, input, studyInstanceUid, instances, null);
                }

                if (!isFailedToUpdateStudy)
                {
                    await context.CallActivityWithRetryAsync(
                        nameof(DeleteOldVersionBlobV3Async),
                        _options.RetryOptions,
                        new CleanupBlobArgumentsV2(instances, input.Partition));

                    await context.CallActivityWithRetryAsync(
                        nameof(SetOriginalBlobToColdAccessTierV2Async),
                        _options.RetryOptions,
                        new CleanupBlobArgumentsV2(instances, input.Partition));
                }
            }

            var numberOfStudyProcessed = input.NumberOfStudyProcessed + 1;

            if (input.TotalNumberOfStudies != numberOfStudyProcessed)
            {
                logger.LogInformation("Completed updating the instances for a study. {Updated}. Continuing with new execution...", instances.Count);
            }

            context.ContinueAsNew(
                new UpdateCheckpoint
                {
                    StudyInstanceUids = input.StudyInstanceUids,
                    ChangeDataset = input.ChangeDataset,
                    Partition = input.Partition,
                    PartitionKey = input.PartitionKey,
                    NumberOfStudyProcessed = numberOfStudyProcessed,
                    NumberOfStudyCompleted = numberStudyUpdated,
                    NumberOfStudyFailed = input.NumberOfStudyFailed,
                    TotalNumberOfInstanceUpdated = totalNoOfInstances,
                    Errors = input.Errors,
                    CreatedTime = input.CreatedTime ?? await context.GetCreatedTimeAsync(_options.RetryOptions),
                });
        }
        else
        {
            if (input.TotalNumberOfInstanceUpdated > 0)
            {
                replaySafeCounter.Add(input.TotalNumberOfInstanceUpdated);
            }

            string serializedInput = GetSerializedCheckpointResult(input);

            if (input.Errors?.Count > 0)
            {
                logger.LogWarning("Update operation completed with errors. {NumberOfStudyProcessed}, {NumberOfStudyUpdated}, {NumberOfStudyFailed}, {TotalNumberOfInstanceUpdated}.",
                     input.NumberOfStudyProcessed,
                     input.NumberOfStudyCompleted,
                     input.NumberOfStudyFailed,
                     input.TotalNumberOfInstanceUpdated);

                _telemetryClient.ForwardOperationLogTrace(
                    "Update operation completed with errors",
                    context.InstanceId,
                    serializedInput,
                    AuditEventSubType.UpdateStudyOperation,
                    ApplicationInsights.DataContracts.SeverityLevel.Error);

                _auditLogger.LogAudit(
                    AuditAction.Executed,
                    AuditEventSubType.UpdateStudyOperation,
                    null,
                    HttpStatusCode.BadRequest,
                    Activity.Current?.RootId,
                    null,
                    null,
                    null);

                // Throwing the exception so that it can set the operation status to Failed
                throw new OperationErrorException("Update operation completed with errors.");
            }
            else
            {
                logger.LogInformation("Update operation completed successfully. {NumberOfStudyProcessed}, {NumberOfStudyUpdated}, {TotalNumberOfInstanceUpdated}.",
                     input.NumberOfStudyProcessed,
                     input.NumberOfStudyCompleted,
                     input.TotalNumberOfInstanceUpdated);

                _telemetryClient.ForwardOperationLogTrace("Update operation completed successfully", context.InstanceId, serializedInput, AuditEventSubType.UpdateStudyOperation);

                _auditLogger.LogAudit(
                    AuditAction.Executed,
                    AuditEventSubType.UpdateStudyOperation,
                    null,
                    HttpStatusCode.OK,
                    Activity.Current?.RootId,
                    null,
                    null,
                    null);
            }
        }
    }

    private async Task HandleException(
        IDurableOrchestrationContext context,
        UpdateCheckpoint input,
        string studyInstanceUid,
        IReadOnlyList<InstanceMetadata> instances,
        IReadOnlyList<string> instanceErrors)
    {
        var errors = new List<string>();

        if (input.Errors != null)
        {
            errors.AddRange(input.Errors);
        }

        errors.Add($"Failed to update instances for study {studyInstanceUid}");

        if (instanceErrors != null)
        {
            // We don't want to populate all the errors in Azure Table Storage, DTFx may attempt to compress the entry as needed using GZip and storing in blob storage
            // But I think we should also be wary of what the user experience is for this via the response, so restricting to 5 errors for now. We can update based on feedback.
            errors.AddRange(instanceErrors.Take(5));

            if (instanceErrors.Count > 5)
            {
                errors.Add("There are more instances failed to update than listed above. Please check the diagnostics logs for the complete list.");
            }

            foreach (string error in instanceErrors)
            {
                _telemetryClient.ForwardOperationLogTrace(error, context.InstanceId, string.Empty, AuditEventSubType.UpdateStudyOperation, ApplicationInsights.DataContracts.SeverityLevel.Error);
            }
        }

        input.Errors = errors;
        input.NumberOfStudyFailed++;

        // Cleanup the new version when the update activity fails
        await TryCleanupActivityV3(context, instances, input.Partition);
    }

    private IReadOnlyList<InstanceMetadata> GetInstanceMetadataList(IReadOnlyList<InstanceMetadata> instanceMetadataList)
    {
        // when external store not enabled, do not update file properties
        return _externalStoreEnabled ? instanceMetadataList : new List<InstanceMetadata>();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Using a generic exception to catch all scenarios.")]
    private async Task TryCleanupActivityV3(IDurableOrchestrationContext context, IReadOnlyList<InstanceMetadata> instances, Partition partition)
    {
        try
        {
            await context.CallActivityWithRetryAsync(
                nameof(CleanupNewVersionBlobV3Async),
                _options.RetryOptions,
                new CleanupBlobArgumentsV2(instances, partition));
        }
        catch (Exception) { }
    }

    private string GetSerializedCheckpointResult(UpdateCheckpoint checkpoint)
    {
        return JsonSerializer.Serialize(new
        {
            checkpoint.StudyInstanceUids,
            partitionName = checkpoint.Partition.Name,
            datasetToUpdate = checkpoint.ChangeDataset,
            checkpoint.NumberOfStudyCompleted,
            checkpoint.NumberOfStudyFailed,
            checkpoint.TotalNumberOfInstanceUpdated,
        }, _jsonSerializerOptions);
    }
}
