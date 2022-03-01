// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Operations.DurableTask
{
    [JsonObject(MemberSerialization.OptIn)]
    internal abstract class OrchestrationManager<T>
    {
        [JsonProperty]
        private bool _pendingPoll;

        [JsonProperty]
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Cannot be readonly as Json.NET needs to set the value after construction.")]
        private HashSet<string> _running;

        private readonly IDurableClient _durableClient;
        private readonly IGuidFactory _guidFactory;
        private readonly Func<DateTime> _getUtcNow;
        private readonly OrchestrationConcurrencyOptions _options;

        public OrchestrationManager(
            IDurableClient durableClient,
            IGuidFactory guidFactory,
            Func<DateTime> getUtcNow,
            IOptions<OrchestrationConcurrencyOptions> options,
            ILogger logger)
        {
            _durableClient = EnsureArg.IsNotNull(durableClient, nameof(durableClient));
            _guidFactory = EnsureArg.IsNotNull(guidFactory, nameof(guidFactory));
            _getUtcNow = EnsureArg.IsNotNull(getUtcNow, nameof(getUtcNow));
            _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
            _running = new HashSet<string>(_options.MaxInstances);
            Logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        protected ILogger Logger { get; }

        public Task<string> StartAsync(StartOrchestrationArgs<T> request)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            // If there is no supplied instance ID, we should backfill one so callers
            // know which instance is performing the actual work when started later.
            if (request.InstanceId == null)
            {
                request = new StartOrchestrationArgs<T>
                {
                    FunctionName = request.FunctionName,
                    Input = request.Input,
                    InstanceId = OperationId.ToString(_guidFactory.Create()),
                };
            }

            Logger.LogInformation("There are currently {Current}/{Max} instances running.", _running.Count, _options.MaxInstances);

            // If we can start now, immediately submit the instance request
            // TODO: Perhaps there should be additional signals
            if (_running.Count < _options.MaxInstances)
            {
                string instanceId = StartOrchestration(request);
                Logger.LogInformation("Submitted execution of orchestration '{FunctionName}' with ID '{InstanceId}'.", request.FunctionName, instanceId);

                EnqueuePollIfActive();
                return Task.FromResult(instanceId);
            }
            else
            {
                string instanceId = DelayOrchestration(request);
                Logger.LogInformation("Queued execution of orchestration '{FunctionName}' with ID '{InstanceId}'.", request.FunctionName, instanceId);

                return Task.FromResult(instanceId);
            }
        }

        public async Task PollRunningAsync()
        {
            Logger.LogInformation("Polling status of orchestration instances.");

            // Determine which instances have finished one way or another, and notify any proxies
            foreach (string instanceId in _running.ToList()) // Shallow snapshot so we can modify it in the loop
            {
                DurableOrchestrationStatus status = await _durableClient.GetStatusAsync(
                    instanceId,
                    showHistory: false,
                    showHistoryOutput: false,
                    showInput: false).ConfigureAwait(false);

                if (status == null)
                {
                    // Perhaps Start and Poll were batched together, so we should wait
                    Logger.LogWarning("Cannot find orchestration instance '{InstanceId}'.", instanceId);
                    continue;
                }

                switch (status.RuntimeStatus)
                {
                    case OrchestrationRuntimeStatus.Completed:
                        Logger.LogInformation("Orchestration instance '{InstanceId}' completed successfully.", instanceId);
                        break;
                    case OrchestrationRuntimeStatus.Failed:
                    case OrchestrationRuntimeStatus.Canceled:
                    case OrchestrationRuntimeStatus.Terminated:
                        Logger.LogWarning("Orchestration instance '{InstanceId}' completed with non-success status '{Status}'.", instanceId, status.RuntimeStatus);
                        break;
                    default:
                        Logger.LogInformation("Orchestration instance '{InstanceId}' is still running with status '{Status}.", instanceId, status.RuntimeStatus);
                        continue;
                }

                // Note: it's safe to re-send these events
                await _durableClient.RaiseEventAsync(instanceId, _options.CompletionEvent, status);

                // Replace the previously running instance with a new one, if any are pending
                _running.Remove(instanceId);
                TryStartNextOrchestration();
            }

            // Schedule the next poll, if necessary
            EnqueuePollIfActive();
        }

        protected abstract string DelayOrchestration(StartOrchestrationArgs<T> request);

        protected abstract string StartOrchestration(StartOrchestrationArgs<T> request);

        protected string StartOrchestration(string functionName, string instanceId, object input)
        {
            // Queue the orchestration to start, but note that this invocation is simply
            // appending a message to a batch of operations. So, the returned ID may not be found
            // in the instance table by the time it's queried.
            //
            // Also note that because this call is fire-and-forget, if this ID already exists
            // (and has already started) it's exception will be ignored which is perfectly fine.
            instanceId = Entity.Current.StartNewOrchestration(functionName, input, instanceId);
            _running.Add(instanceId);

            return instanceId;
        }

        protected abstract bool TryStartNextOrchestration();

        private void EnqueuePollIfActive()
        {
            if (_running.Count == 0)
            {
                _pendingPoll = false;
                Logger.LogInformation("There are no running orchestration instances. Polling is not necessary.");
            }
            else if (_pendingPoll)
            {
                Logger.LogWarning("Poll already pending.");
            }
            else
            {
                DateTime next = _getUtcNow() + _options.PollingInterval;
                Entity.Current.SignalEntity(Entity.Current.EntityId, next, nameof(PollRunningAsync));
                _pendingPoll = true;
                Logger.LogInformation("Will poll again for orchestration instance statuses at '{Timestamp}'.", next);
            }
        }
    }
}
