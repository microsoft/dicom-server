// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;

namespace Microsoft.Health.Dicom.Operations.DurableTask
{
    internal abstract class OrchestrationManager : IOrchestrationManager
    {
        private bool _pendingPoll;
        private readonly HashSet<string> _running;
        private readonly Queue<OrchestrationSpecification> _pending = new Queue<OrchestrationSpecification>();

        private readonly IDurableClient _durableClient;
        private readonly IGuidFactory _guidFactory;
        private readonly Func<DateTime> _getUtcNow;
        private readonly OrchestrationConcurrencyOptions _options;
        private readonly ILogger _logger;

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
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
            _running = new HashSet<string>(_options.MaxInstances);
        }

        public Task<string> StartAsync(OrchestrationRequest<object> request)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            if (_running.Count < _options.MaxInstances)
            {
                string instanceId = QueueStart(request.FunctionName, request.InstanceId, request.Input);
                TryQueuePoll();
                return Task.FromResult(instanceId);
            }
            else
            {
                // If there is no supplied instance ID, we should backfill one so callers
                // know which instance is performing the actual work when started later.
                var spec = new OrchestrationSpecification
                {
                    FunctionName = request.FunctionName,
                    Input = request.Input,
                    InstanceId = request.InstanceId ?? OperationId.ToString(_guidFactory.Create()),
                };

                _pending.Enqueue(spec);

                _logger.LogInformation("Queued execution of orchestration '{FunctionName}' with ID '{InstanceId}'.", spec.FunctionName, spec.InstanceId);
                return Task.FromResult(spec.InstanceId);
            }
        }

        public async Task PollRunningAsync()
        {
            _logger.LogInformation("Polling status of orchestration instances.");

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
                    _logger.LogWarning("Cannot find orchestration instance '{InstanceId}'.", instanceId);
                    continue;
                }

                switch (status.RuntimeStatus)
                {
                    case OrchestrationRuntimeStatus.Completed:
                        _logger.LogInformation("Orchestration instance '{InstanceId}' completed successfully.", instanceId);
                        break;
                    case OrchestrationRuntimeStatus.Failed:
                    case OrchestrationRuntimeStatus.Canceled:
                    case OrchestrationRuntimeStatus.Terminated:
                        _logger.LogWarning("Orchestration instance '{InstanceId}' completed with non-success status '{Status}'.", instanceId, status.RuntimeStatus);
                        break;
                    default:
                        _logger.LogInformation("Orchestration instance '{InstanceId}' is still running with status '{Status}.", instanceId, status.RuntimeStatus);
                        continue;
                }

                // Note: it's safe to re-send these events
                await _durableClient.RaiseEventAsync(instanceId, _options.CompletionEvent, status);

                // Replace the previously running instance with a new one, if any are pending
                _running.Remove(instanceId);
                if (_pending.TryDequeue(out OrchestrationSpecification next))
                {
                    QueueStart(next.FunctionName, next.InstanceId, next.Input);
                }
            }

            // Schedule the next poll, if necessary
            if (_running.Count > 0)
            {
                TryQueuePoll();
            }
            else
            {
                _pendingPoll = false;
            }
        }

        private string QueueStart(string functionName, string instanceId, object input)
        {
            // Queue the orchestration to start, but note that this invocation is simply
            // appending a message to a batch of operations. So, the returned ID may not be found
            // in the instance table by the time it's queried.
            //
            // Also note that because this call is fire-and-forget, if this ID already exists
            // (and has already started) it's exception will be ignored which is perfectly fine.
            instanceId = Entity.Current.StartNewOrchestration(functionName, input, instanceId);
            _running.Add(instanceId);

            _logger.LogInformation("Submitted execution of orchestration '{FunctionName}' with ID '{InstanceId}'.", functionName, instanceId);
            return instanceId;
        }

        private bool TryQueuePoll()
        {
            if (!_pendingPoll)
            {
                _pendingPoll = true;

                DateTime next = _getUtcNow() + _options.PollingInterval;
                Entity.Current.SignalEntity(Entity.Current.EntityId, next, nameof(PollRunningAsync));

                _logger.LogInformation("Will poll again for orchestration instance statuses at '{Timestamp}'.", next);
                return true;
            }

            return false;
        }

        private readonly struct OrchestrationSpecification
        {
            public string FunctionName { get; init; }

            public string InstanceId { get; init; }

            public object Input { get; init; }
        }
    }
}
