// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.Export.Models;

namespace Microsoft.Health.Dicom.Functions.Export;

public partial class ExportDurableFunction
{
    [FunctionName(nameof(ExportBatchAsync))]
    public async Task<int> ExportBatchAsync([ActivityTrigger] ExportBatchArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Exporting DCM files starting from {Offset} to '{Sink}'.", arguments.Batch.Offset, arguments.SinkDescription.Name);

        IExportSink sink = _sinkFactory.CreateSink(arguments.SinkDescription);

        int count = 0;
        IAsyncEnumerator<VersionedInstanceIdentifier> source = arguments.Batch.GetAsyncEnumerator();
        do
        {
            Task[] exportTasks = await GetNextAsync(source, _options.BatchThreadCount)
                .Select(x => sink.CopyAsync(x))
                .ToArrayAsync();

            await Task.WhenAll(exportTasks);
            count += exportTasks.Length;

        } while (await source.MoveNextAsync());

        logger.LogInformation("Successfully exported {Count} DCM files.", count);
        return count;
    }

    private static async IAsyncEnumerable<T> GetNextAsync<T>(IAsyncEnumerator<T> source, int n)
    {
        for (int i = 0; i < n || n == -1; i++)
        {
            if (!await source.MoveNextAsync())
                yield break;

            yield return source.Current;
        }
    }
}
