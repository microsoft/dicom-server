// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Operations.DurableTask
{
    [JsonObject(MemberSerialization.OptIn)]
    internal abstract class OrchestrationThrottle : OrchestrationManager<object>, IOrchestrationThrottle
    {
        [JsonProperty]
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Cannot be readonly as Json.NET needs to set the value after construction.")]
        private Queue<StartOrchestrationArgs<object>> _pending = new Queue<StartOrchestrationArgs<object>>();

        public OrchestrationThrottle(
            IDurableClient durableClient,
            IGuidFactory guidFactory,
            Func<DateTime> getUtcNow,
            IOptions<OrchestrationConcurrencyOptions> options,
            ILogger logger)
            : base(durableClient, guidFactory, getUtcNow, options, logger)
        { }

        protected override string DelayOrchestration(StartOrchestrationArgs<object> request)
        {
            _pending.Enqueue(new StartOrchestrationArgs<object>()
            {
                FunctionName = request.FunctionName,
                Input = request.Input,
                InstanceId = request.InstanceId,
            });

            return request.InstanceId;
        }

        protected override bool TryStartNextOrchestration()
        {
            if (_pending.TryDequeue(out StartOrchestrationArgs<object> args))
            {
                StartOrchestration(args.FunctionName, args.InstanceId, args.Input);
                return true;
            }

            return false;
        }

        protected override string StartOrchestration(StartOrchestrationArgs<object> request)
            => StartOrchestration(request.FunctionName, request.InstanceId, request.Input);
    }
}
