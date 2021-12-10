// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Functions
{
    public sealed class JobHostExecution : IJobHost, IAsyncDisposable
    {
        private readonly IJobHost _jobHost;

        public JobHostExecution(IJobHost jobHost)
            => _jobHost = EnsureArg.IsNotNull(jobHost, nameof(jobHost));

        public Task CallAsync(string name, IDictionary<string, object> arguments = null, CancellationToken cancellationToken = default)
            => _jobHost.CallAsync(name, arguments, cancellationToken);

        public Task StartAsync(CancellationToken cancellationToken)
            => _jobHost.StartAsync(cancellationToken);

        public Task StopAsync()
            => _jobHost.StopAsync();

        public ValueTask DisposeAsync()
            => new ValueTask(_jobHost.StopAsync());
    }
}
