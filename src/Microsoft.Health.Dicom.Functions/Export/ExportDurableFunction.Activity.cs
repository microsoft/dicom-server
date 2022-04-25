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

            // Compute success metrics
            if (exports.Length > 0)
            {
                progress = exports.Aggregate<bool, ExportProgress>(
                    default,
                    (state, success) => success
                        ? new ExportProgress(state.Exported + 1, state.Failed)
                        : new ExportProgress(state.Exported, state.Failed + 1));

                logger.LogInformation("Successfully exported {Files} DCM files.", progress.Exported);
                if (progress.Failed > 0)
                {
                    logger.LogWarning("Failed to export {Files} DCM files.", progress.Failed);
                }
            }
        } while (exports.Length > 0);

        return progress;
    }

    [FunctionName(nameof(GetErrorHrefAsync))]
    public async Task<Uri> GetErrorHrefAsync([ActivityTrigger] IDurableActivityContext context)
    {
        EnsureArg.IsNotNull(context, nameof(context));

        TypedConfiguration<ExportDestinationType> destination = context.GetInput<TypedConfiguration<ExportDestinationType>>();
        await using IExportSink sink = _sinkFactory.CreateSink(destination, context.GetOperationId());
        return sink.ErrorHref;
    }
}
