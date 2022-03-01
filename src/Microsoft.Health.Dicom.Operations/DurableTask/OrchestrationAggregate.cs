// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Operations.DurableTask
{
    [JsonObject(MemberSerialization.OptIn)]
    internal abstract class OrchestrationAggregate<TIn, TState, TOut> : OrchestrationManager<TIn>, IOrchestrationAggregate<TIn>
    {
        [JsonProperty]
        private StartOrchestrationArgs<TState> _pending;

        public OrchestrationAggregate(
            IDurableClient durableClient,
            IGuidFactory guidFactory,
            Func<DateTime> getUtcNow,
            IOptions<OrchestrationConcurrencyOptions> options,
            ILogger logger)
            : base(durableClient, guidFactory, getUtcNow, options, logger)
        { }

        protected override string DelayOrchestration(StartOrchestrationArgs<TIn> request)
        {
            _pending = _pending == null
                ? new StartOrchestrationArgs<TState>
                {
                    FunctionName = request.FunctionName,
                    Input = Add(Initialize(), request.Input),
                    InstanceId = request.InstanceId,
                }
                : new StartOrchestrationArgs<TState>
                {
                    FunctionName = _pending.FunctionName,
                    Input = Add(_pending.Input, request.Input),
                    InstanceId = _pending.InstanceId,
                };

            return _pending.InstanceId;
        }

        protected override bool TryStartNextOrchestration()
        {
            if (_pending != null)
            {
                StartOrchestration(_pending.FunctionName, _pending.InstanceId, Finalize(_pending.Input));
                _pending = null;
                return true;
            }

            return false;
        }

        protected override string StartOrchestration(StartOrchestrationArgs<TIn> request)
            => StartOrchestration(request.FunctionName, request.InstanceId, Finalize(Add(Initialize(), request.Input)));

        protected abstract TState Add(TState state, TIn input);

        protected abstract TOut Finalize(TState state);

        protected abstract TState Initialize();
    }
}
