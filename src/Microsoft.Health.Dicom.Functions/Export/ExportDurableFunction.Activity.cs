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
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Functions.Export.Models;

namespace Microsoft.Health.Dicom.Functions.Export;

public partial class ExportDurableFunction
{
    [FunctionName(nameof(ExportBatchAsync))]
    public async Task<ExportResult> ExportBatchAsync([ActivityTrigger] ExportBatchArguments args, ILogger logger)
    {
        EnsureArg.IsNotNull(args, nameof(args));
        EnsureArg.IsNotNull(logger, nameof(logger));

        await using IExportSource source = _sourceFactory.CreateSource(args.Source);
        await using IExportSink sink = _sinkFactory.CreateSink(args.Destination);

        // Export
        Task<bool>[] exportTasks = await source.Select(x => TryCopyAsync(x, sink, logger)).ToArrayAsync();

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

    private static async Task<bool> TryCopyAsync(VersionedInstanceIdentifier identifier, IExportSink sink, ILogger logger)
    {
        try
        {
            await sink.CopyAsync(identifier);
            return true;
        }
        catch (DataStoreException dse) // TODO: Change exception
        {
            logger.LogError(dse, "Unable to copy watermark {Watermark}", identifier.Version);
            // union and send the formatted error
            //await sink.AppendErrorAsync(identifier, dse);
            return false;
        }
    }
}
