// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common;

internal sealed class NullHost : IHost
{
    public static IHost Instance { get; } = new NullHost();

    public IServiceProvider Services => NullServiceProvider.Instance;

    private NullHost()
    { }

    public void Dispose()
    { }

    public Task StartAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    private sealed class NullServiceProvider : IServiceProvider
    {
        public static IServiceProvider Instance { get; } = new NullServiceProvider();

        private NullServiceProvider()
        { }

        public object GetService(Type serviceType)
            => null;
    }
}
