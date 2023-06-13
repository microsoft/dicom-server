// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Functions.Export.Models;
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.Export;

public partial class ExportDurableFunction
{
    /// <summary>
    /// Asynchronously exports a batch of DICOM files to a user-specified sink.
    /// </summary>
    /// <param name="context">The context for the activity.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="ExportBatchAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property contains the number a summary of the export
    /// operation's progress.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(ExportBatchAsync))]
    public async Task<ExportProgress> ExportBatchAsync([ActivityTrigger] IDurableActivityContext context, ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsNotNull(logger, nameof(logger));

        ExportBatchArguments args = context.GetInput<ExportBatchArguments>();
        await using IExportSource source = await _sourceFactory.CreateAsync(args.Source, args.Partition); // todo get partition name from here
        await using IExportSink sink = await _sinkFactory.CreateAsync(args.Destination, context.GetOperationId());

        source.ReadFailure += (source, e) => logger.LogError(e.Exception, "Cannot read desired DICOM file(s)");
        sink.CopyFailure += (source, e) => logger.LogError(e.Exception, "Unable to copy watermark {Watermark}", e.Identifier.Version);

        // Copy files
        int successes = 0, failures = 0;
        await Parallel.ForEachAsync(
                source,
                new ParallelOptions
                {
                    CancellationToken = default,
                    MaxDegreeOfParallelism = _options.MaxParallelThreads,
                },
                async (result, token) =>
                {
                    if (await sink.CopyAsync(result, token))
                        Interlocked.Increment(ref successes);
                    else
                        Interlocked.Increment(ref failures);
                });

        // Flush any files or errors
        await sink.FlushAsync();

        logger.LogInformation("Successfully exported {Files} DCM files.", successes);
        if (failures > 0)
            logger.LogWarning("Failed to export {Files} DCM files.", failures);

        return new ExportProgress(successes, failures);
    }

    /// <summary>
    /// Asynchronously completes a copy operation to the sink.
    /// </summary>
    /// <param name="destination">The options for a specific sink type.</param>
    /// <returns>A task representing the <see cref="CompleteCopyAsync"/> operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> is <see langword="null"/>.</exception>
    [FunctionName(nameof(CompleteCopyAsync))]
    public Task CompleteCopyAsync([ActivityTrigger] ExportDataOptions<ExportDestinationType> destination)
    {
        EnsureArg.IsNotNull(destination, nameof(destination));
        return _sinkFactory.CompleteCopyAsync(destination);
    }
}
