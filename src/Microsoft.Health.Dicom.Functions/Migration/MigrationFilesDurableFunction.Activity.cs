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
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Functions.Migration.Models;

namespace Microsoft.Health.Dicom.Functions.Migration;

public partial class MigrationFilesDurableFunction
{
    /// <summary>
    /// Asynchronously retrieves the next set of instance batches based on the configured options.
    /// </summary>
    /// <param name="arguments">The options for configuring the batches.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the asynchronous get operation. The value of its <see cref="Task{TResult}.Result"/>
    /// property contains a list of batches as defined by their smallest and largest watermark.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(GetInstanceBatchesByTimeStampAsync))]
    public Task<IReadOnlyList<WatermarkRange>> GetInstanceBatchesByTimeStampAsync(
        [ActivityTrigger] MigrationBatchCreationArguments arguments,
        ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(logger, nameof(logger));

        if (arguments.MaxWatermark.HasValue)
        {
            logger.LogInformation("Dividing up the instances into batches starting from the largest watermark {Watermark}.", arguments.MaxWatermark);
        }
        else
        {
            logger.LogInformation("Dividing up the instances into batches starting from the end.");
        }

        return _instanceStore.GetInstanceBatchesByTimeStampAsync(
            arguments.BatchSize,
            arguments.MaxParallelBatches,
            IndexStatus.Created,
            arguments.StartFilterTimeStamp,
            arguments.EndFilterTimeStamp,
            arguments.MaxWatermark,
            CancellationToken.None);
    }

    /// <summary>
    /// Asynchronously migrate frame range files
    /// </summary>
    /// <param name="watermarkRange">The options that include the instances to re-index and the query tags.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>A task representing the <see cref="MigrateFrameRangeFilesAsync"/> operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(MigrateFrameRangeFilesAsync))]
    public async Task MigrateFrameRangeFilesAsync([ActivityTrigger] WatermarkRange watermarkRange, ILogger logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));

        IReadOnlyList<VersionedInstanceIdentifier> instanceIdentifiers =
            await _instanceStore.GetInstanceIdentifiersByWatermarkRangeAsync(watermarkRange, IndexStatus.Created);

        var versions = instanceIdentifiers.Select(x => x.Version).ToList();

        await Parallel.ForEachAsync(
            versions,
            new ParallelOptions
            {
                CancellationToken = default,
                MaxDegreeOfParallelism = _options.MaxParallelThreads,
            },
            (version, token) =>
            {
                return new ValueTask(_metadataStore.CopyInstanceFramesRangeAsync(version, token));
            });

        logger.LogInformation("Completed copying frame range files in the range {Range}.", watermarkRange);

        await Parallel.ForEachAsync(
            versions,
            new ParallelOptions
            {
                CancellationToken = default,
                MaxDegreeOfParallelism = _options.MaxParallelThreads,
            },
            (version, token) =>
            {
                return new ValueTask(_metadataStore.DeleteMigratedFramesRangeIfExistsAsync(version, token));
            });

        logger.LogInformation("Completed deleting frame range files with space in the range {Range}.", watermarkRange);
    }
}
