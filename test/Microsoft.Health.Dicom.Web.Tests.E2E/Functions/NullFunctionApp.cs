// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Functions
{
    internal sealed class NullFunctionApp : IFunctionApp
    {
        public static IFunctionApp Instance { get; } = new NullFunctionApp();

        private NullFunctionApp()
        { }

        public ValueTask<JobHostExecution> StartAsync()
            => ValueTask.FromResult(new JobHostExecution(NullJobHost.Instance));

        private sealed class NullJobHost : IJobHost
        {
            public static IJobHost Instance { get; } = new NullJobHost();

            private NullJobHost()
            { }

            public Task CallAsync(string name, IDictionary<string, object> arguments = null, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task StartAsync(CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task StopAsync()
                => Task.CompletedTask;
        }
    }
}
