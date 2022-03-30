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
using Microsoft.Health.Dicom.Functions.Export.Models;

namespace Microsoft.Health.Dicom.Functions.Export;

public partial class ExportDurableFunction
{
    [FunctionName(nameof(ExportBatchAsync))]
    public async Task<ExportResult> ExportBatchAsync([ActivityTrigger] ExportBatchArguments args, ILogger logger)
    {
        EnsureArg.IsNotNull(args, nameof(args));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Exporting DCM files starting from {Offset} to '{Sink}'.", args.Offset, args.Destination.Type);

        IExportSource source = _sourceFactory.CreateSource(args.Source);
        IExportSink sink = _sinkFactory.CreateSink(args.Destination);

        // Get the batch
        IExportBatch batch = await source.GetBatchAsync(args.Offset);

        // Export
        Task<bool>[] exportTasks = await batch.Select(x => TryCopyAsync(x, sink, logger)).ToArrayAsync();

        // Compute success metrics
        bool[] results = await Task.WhenAll(exportTasks);
        return results.Aggregate(
            (Exported: 0, Failed: 0),
            (state, success) => success ? (state.Exported + 1, state.Failed) : (state.Exported, state.Failed + 1),
            state => new ExportResult { Exported = state.Exported, Failed = state.Failed, });
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
