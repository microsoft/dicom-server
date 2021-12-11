// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Functions
{
    internal sealed class NullFunctionApp : IFunctionApp
    {
        public static IFunctionApp Instance { get; } = new NullFunctionApp();

        private NullFunctionApp()
        { }

        public ValueTask<IAsyncDisposable> StartAsync()
            => ValueTask.FromResult(NullAsyncDisposable.Instance);

        public ValueTask StopAsync()
            => ValueTask.CompletedTask;

        private sealed class NullAsyncDisposable : IAsyncDisposable
        {
            public static IAsyncDisposable Instance { get; } = new NullAsyncDisposable();

            private NullAsyncDisposable()
            { }

            public ValueTask DisposeAsync()
                => ValueTask.CompletedTask;
        }
    }
}
