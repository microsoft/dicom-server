// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.Update.Models;

namespace Microsoft.Health.Dicom.Functions.Update;

public partial class UpdateDurableFunction
{
    /// <summary>
    /// Asynchronously update instance new watermark.
    /// </summary>
    /// <param name="arguments">BatchUpdateArguments</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="UpdateInstanceWatermarkAsync"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(UpdateInstanceWatermarkAsync))]
    public async Task<IReadOnlyList<InstanceFileState>> UpdateInstanceWatermarkAsync([ActivityTrigger] UpdateInstanceWatermarkArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.StudyInstanceUid, nameof(arguments.StudyInstanceUid));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Beginning to update all instance watermarks");

        IEnumerable<InstanceMetadata> instanceMetadata = await _indexStore.BeginUpdateInstancesAsync(arguments.PartitionKey, arguments.StudyInstanceUid, CancellationToken.None);

        logger.LogInformation("Beginning to update all instance watermarks");

        return instanceMetadata.Select(x =>
            new InstanceFileState
            {
                Version = x.VersionedInstanceIdentifier.Version,
                OriginalVersion = x.InstanceProperties.OriginalVersion,
                NewVersion = x.InstanceProperties.NewVersion
            }).ToList();
    }

    /// <summary>
    /// Asynchronously batches the instance watermarks and calls the update instance.
    /// </summary>
    /// <param name="arguments">BatchUpdateArguments</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="UpdateInstanceBlobsAsync"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(UpdateInstanceBlobsAsync))]
    public async Task UpdateInstanceBlobsAsync([ActivityTrigger] UpdateInstanceBlobArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.ChangeDataset, nameof(arguments.ChangeDataset));
        EnsureArg.IsNotNull(arguments.InstanceWatermarks, nameof(arguments.InstanceWatermarks));
        EnsureArg.IsNotNull(logger, nameof(logger));

        DicomDataset datasetToUpdate = GetDeserialzedDataset(arguments.ChangeDataset);

        int processed = 0;

        logger.LogInformation("Beginning to update all instance blobs, Total count {TotalCount}", arguments.InstanceWatermarks.Count);

        while (processed < arguments.InstanceWatermarks.Count)
        {
            int batchSize = Math.Min(_options.BatchSize, arguments.InstanceWatermarks.Count - processed);
            var batch = arguments.InstanceWatermarks.Skip(processed).Take(batchSize).ToList();

            logger.LogInformation("Beginning to update instance blobs for range [{Start}, {End}]. Total batch size {BatchSize}.",
                    batch[0],
                    batch[^1],
                    batchSize);

            await Parallel.ForEachAsync(
                batch,
                new ParallelOptions
                {
                    CancellationToken = default,
                    MaxDegreeOfParallelism = _options.MaxParallelThreads,
                },
                async (instance, token) =>
                {
                    await _updateInstanceService.UpdateInstanceBlobAsync(instance, datasetToUpdate, token);
                });

            logger.LogInformation("Completed updating instance blobs starting with [{Start}, {End}]. Total batchSize {BatchSize}.",
                batch[0],
                batch[^1],
                batchSize);

            processed += batchSize;
        }

        logger.LogInformation("Completed updating all instance blobs");
    }

    /// <summary>
    /// Asynchronously commits all the instances in a study and creates new entries for changefeed.
    /// </summary>
    /// <param name="arguments">CompleteInstanceArguments</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="CompleteUpdateStudyAsync"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(CompleteUpdateStudyAsync))]
    public async Task CompleteUpdateStudyAsync([ActivityTrigger] CompleteStudyArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(arguments.ChangeDataset, nameof(arguments.ChangeDataset));
        EnsureArg.IsNotNull(arguments.StudyInstanceUid, nameof(arguments.StudyInstanceUid));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Completing updating operation for study.");

        try
        {
            await _indexStore.EndUpdateInstanceAsync(
                arguments.PartitionKey,
                arguments.StudyInstanceUid,
                GetDeserialzedDataset(arguments.ChangeDataset),
                CancellationToken.None);

            logger.LogInformation("Updating study completed successfully.");
        }
        catch (StudyNotFoundException)
        {
            // TODO: Study deleted, we need to cleanup all the newly updated blobs.
            logger.LogWarning("Failed to update to study. Possibly deleted.");
        }
    }

    /// <summary>
    /// Asynchronously delete all the old blobs if it has more than 2 version.
    /// </summary>
    /// <param name="context">Activity context which has list of watermarks to cleanup</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="DeleteOldVersionBlobAsync"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(DeleteOldVersionBlobAsync))]
    public async Task DeleteOldVersionBlobAsync([ActivityTrigger] IDurableActivityContext context, ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsNotNull(logger, nameof(logger));

        IReadOnlyList<InstanceFileState> fileIdentifiers = context.GetInput<IReadOnlyList<InstanceFileState>>();
        int fileCount = fileIdentifiers.Where(f => f.OriginalVersion.HasValue).Count();

        logger.LogInformation("Begin deleting old blobs. Total size {TotalCount}", fileCount);

        await Parallel.ForEachAsync(
            fileIdentifiers.Where(f => f.OriginalVersion.HasValue),
            new ParallelOptions
            {
                CancellationToken = default,
                MaxDegreeOfParallelism = _options.MaxParallelThreads,
            },
            async (fileIdentifier, token) =>
            {
                await _updateInstanceService.DeleteInstanceBlobAsync(fileIdentifier.Version, token);
            });

        logger.LogInformation("Old blobs deleted successfully. Total size {TotalCount}", fileCount);
    }

    /// <summary>
    /// Asynchronously delete the new blob when there is a failure while updating the study instances.
    /// </summary>
    /// <param name="context">Activity context which has list of watermarks to cleanup</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="CleanupNewVersionBlobAsync"/> operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(CleanupNewVersionBlobAsync))]
    public async Task CleanupNewVersionBlobAsync([ActivityTrigger] IDurableActivityContext context, ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsNotNull(logger, nameof(logger));

        IReadOnlyList<InstanceFileState> fileIdentifiers = context.GetInput<IReadOnlyList<InstanceFileState>>();

        int fileCount = fileIdentifiers.Where(f => f.NewVersion.HasValue).Count();
        logger.LogInformation("Begin cleaning up new blobs. Total size {TotalCount}", fileCount);

        await Parallel.ForEachAsync(
            fileIdentifiers.Where(f => f.NewVersion.HasValue),
            new ParallelOptions
            {
                CancellationToken = default,
                MaxDegreeOfParallelism = _options.MaxParallelThreads,
            },
            async (fileIdentifier, token) =>
            {
                await _updateInstanceService.DeleteInstanceBlobAsync(fileIdentifier.NewVersion.Value, token);
            });

        logger.LogInformation("New blobs deleted successfully. Total size {TotalCount}", fileCount);
    }

    private DicomDataset GetDeserialzedDataset(string dataset) => JsonSerializer.Deserialize<DicomDataset>(dataset, _jsonSerializerOptions);
}
