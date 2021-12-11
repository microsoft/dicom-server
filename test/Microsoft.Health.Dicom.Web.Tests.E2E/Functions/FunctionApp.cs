// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Functions
{
    internal sealed class FunctionApp : IFunctionApp
    {
        private readonly IHost _host;

        public FunctionApp(IHost host)
            => _host = EnsureArg.IsNotNull(host, nameof(host));

        public async ValueTask<IAsyncDisposable> StartAsync()
        {
            await _host.StartAsync(default);
            return new HostExecution(_host);
        }

        public ValueTask StopAsync()
            => new ValueTask(_host.StopAsync(default));

        private sealed class HostExecution : IAsyncDisposable
        {
            private readonly IHost _host;

            public HostExecution(IHost host)
                => _host = EnsureArg.IsNotNull(host, nameof(host));

            public ValueTask DisposeAsync()
                => new ValueTask(_host.StopAsync(default));
        }
    }
}
