// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Operations.DurableTask
{
    internal abstract class OrchestrationAggregator<TIn, TState, TOut> : IOrchestrationManager<TIn>
    {
        protected abstract TState Initialize();

        protected abstract TState Add(TIn input, TState state);

        protected abstract TOut Finalize(TState state);

        public Task<string> StartAsync(OrchestrationRequest<TIn> request)
        {
            throw new NotImplementedException();
        }
    }
}
