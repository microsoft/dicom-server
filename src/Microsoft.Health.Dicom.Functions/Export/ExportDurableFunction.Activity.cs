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
using Microsoft.Health.Dicom.Functions.Export.Models;

namespace Microsoft.Health.Dicom.Functions.Export;

public partial class ExportDurableFunction
{
    [FunctionName(nameof(ExportBatchAsync))]
    public async Task<int> ExportBatchAsync([ActivityTrigger] ExportBatchArguments args, ILogger logger)
    {
        EnsureArg.IsNotNull(args, nameof(args));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Exporting DCM files starting from {Offset} to '{Sink}'.", args.Offset, args.Destination.Type);

        IExportSource source = _sourceFactory.CreateSource(args.Source);
        IExportSink sink = _sinkFactory.CreateSink(args.Destination);

        // Get the batch
        IExportBatch batch = await source.GetBatchAsync(args.Batching.Size, args.Offset);

        // Export
        Task[] exportTasks = await batch.Select(x => sink.CopyAsync(x)).ToArrayAsync();
        await Task.WhenAll(exportTasks);

        return exportTasks.Length;
    }
}
