// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Functions
{
    internal class HostLifecycleService : IApplicationLifetimeWrapper
    {
        internal static readonly IApplicationLifetimeWrapper NoOp = new NoOpLifetimeWrapper();

        private readonly IHostApplicationLifetime _appLifetime;

        public HostLifecycleService(IHostApplicationLifetime appLifetime)
            => _appLifetime = EnsureArg.IsNotNull(appLifetime, nameof(appLifetime));

        public CancellationToken OnStarted => _appLifetime.ApplicationStarted;

        public CancellationToken OnStopping => _appLifetime.ApplicationStopping;

        public CancellationToken OnStopped => _appLifetime.ApplicationStopped;

        private class NoOpLifetimeWrapper : IApplicationLifetimeWrapper
        {
            public CancellationToken OnStarted => CancellationToken.None;

            public CancellationToken OnStopping => CancellationToken.None;

            public CancellationToken OnStopped => CancellationToken.None;
        }
    }
}
