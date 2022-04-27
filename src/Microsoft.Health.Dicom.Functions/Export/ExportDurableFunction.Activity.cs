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
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Functions.Export.Models;
using Microsoft.Health.Dicom.Functions.Extensions;
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
        await using IExportSource source = _sourceFactory.CreateSource(args.Source);
        await using IExportSink sink = _sinkFactory.CreateSink(args.Destination, context.GetOperationId());

        // Export
        source.ReadFailure += (source, e) => logger.LogError(e.Exception, "Cannot read desired DICOM file(s)");
        sink.CopyFailure += (source, e) => logger.LogError(e.Exception, "Unable to copy watermark {Watermark}", e.Identifier.Version);

        bool[] exports;
        ExportProgress progress = default;
        IAsyncEnumerator<ReadResult> sourceEnumerator = source.GetAsyncEnumerator();
        do
        {
            // Only process a subset of the batch at a time based on the desired number of threads
            exports = await Task.WhenAll(
                await sourceEnumerator
                    .Take(_options.BatchThreadCount)
                    .Select(x => sink.CopyAsync(x))
                    .ToArrayAsync());

            progress += exports
                .Select(success => success ? new ExportProgress(1, 0) : new ExportProgress(0, 1))
                .Aggregate(default(ExportProgress), (x, y) => x + y);
        } while (exports.Length > 0);

        logger.LogInformation("Successfully exported {Files} DCM files.", progress.Exported);
        if (progress.Failed > 0)
        {
            logger.LogWarning("Failed to export {Files} DCM files.", progress.Failed);
        }

        return progress;
    }

    /// <summary>
    /// Asynchronously retrieves the URI for the error resource in the user-specified sink.
    /// </summary>
    /// <param name="context">The context for the activity.</param>
    /// <returns>
    /// A task representing the <see cref="ExportBatchAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property contains the URI.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    [FunctionName(nameof(GetErrorHrefAsync))]
    public async Task<Uri> GetErrorHrefAsync([ActivityTrigger] IDurableActivityContext context)
    {
        EnsureArg.IsNotNull(context, nameof(context));

        TypedConfiguration<ExportDestinationType> destination = context.GetInput<TypedConfiguration<ExportDestinationType>>();
        await using IExportSink sink = _sinkFactory.CreateSink(destination, context.GetOperationId());
        return sink.ErrorHref;
    }
}
