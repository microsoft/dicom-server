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
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.CustomTag;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Reindex
{
    /// <summary>
    /// The worker responsible for running the reindex job tasks.
    /// </summary>
    public class CustomTagJobWorker
    {
        private readonly ILogger<CustomTagJobWorker> _logger;
        private readonly ICustomTagJobStore _customTagJobStore;
        private const int Delay = 30000;

        public CustomTagJobWorker(ICustomTagJobStore customTagJobStore, ILogger<CustomTagJobWorker> logger)
        {
            EnsureArg.IsNotNull(customTagJobStore, nameof(customTagJobStore));
            EnsureArg.IsNotNull(logger, nameof(logger));
            _customTagJobStore = customTagJobStore;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // only 1 job for now
            List<Task> runningTasks = new List<Task>();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    runningTasks.RemoveAll(task => task.IsCompleted);

                    // only support 1 job
                    if (runningTasks.Count > 0)
                    {
                        await Task.Delay(Delay, cancellationToken);
                        continue;
                    }

                    IEnumerable<CustomTagJob> customTagJobs = await _customTagJobStore.AcquireCustomTagJobsAsync(1, cancellationToken);

                    if (customTagJobs.Count() == 0)
                    {
                        await Task.Delay(Delay, cancellationToken);
                        continue;
                    }

                    foreach (CustomTagJob job in customTagJobs)
                    {
                        _logger.LogTrace($"Picked up reindex job: {job.Key}.");

                        Task task;
                        task = CreateTask(job, cancellationToken);
                        runningTasks.Add(task);
                    }

                    await Task.Delay(Delay, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // End the execution of the task
                }
                catch (Exception ex)
                {
                    // The job failed.
                    _logger.LogError(ex, "Unhandled exception in the worker.");
                    await Task.Delay(Delay, cancellationToken);
                }
            }
        }

        private static Task CreateTask(CustomTagJob job, CancellationToken cancellationToken)
        {
            Task task;
            switch (job.Type)
            {
                case CustomTagJobType.Reindexing:
                    task = new ReindexJobTask().ExecuteAsync(job, cancellationToken);
                    break;
                case CustomTagJobType.Deindexing:
                    task = new DeindexJobTask().ExecuteAsync(job, cancellationToken);
                    break;
                default:
                    throw new NotSupportedException();
            }

            return task;
        }
    }
}
