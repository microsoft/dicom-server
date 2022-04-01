// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
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
    [FunctionName(nameof(ExportBatchAsync))]
    public async Task<ExportResult> ExportBatchAsync([ActivityTrigger] IDurableActivityContext context, ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsNotNull(logger, nameof(logger));

        ExportBatchArguments args = context.GetInput<ExportBatchArguments>();
        await using IExportSource source = _sourceFactory.CreateSource(args.Source);
        await using IExportSink sink = _sinkFactory.CreateSink(args.Destination, context.GetOperationId());

        // Export
        sink.CopyFailure += (source, e) => logger.LogError(e.Exception, "Unable to copy watermark {Watermark}", e.Identifier.Version);
        Task<bool>[] exportTasks = await source.Select(x => sink.CopyAsync(x)).ToArrayAsync();

        // Compute success metrics
        bool[] results = await Task.WhenAll(exportTasks);
        ExportResult result = results.Aggregate<bool, ExportResult>(
            default,
            (state, success) => success
                ? new ExportResult(state.Exported + 1, state.Failed)
                : new ExportResult(state.Exported, state.Failed + 1));

        logger.LogInformation("Successfully exported {Files} DCM files.", result.Exported);
        if (result.Failed > 0)
        {
            logger.LogWarning("Failed to export {Files} DCM files.", result.Failed);
        }

        return result;
    }
}
