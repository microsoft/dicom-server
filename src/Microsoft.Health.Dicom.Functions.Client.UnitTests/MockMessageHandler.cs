// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests
{
    internal sealed class MockMessageHandler : HttpMessageHandler
    {
        public event Func<HttpRequestMessage, CancellationToken, Task> SendingAsync;

        public int SentMessages { get; private set; }

        private readonly HttpResponseMessage _expected;

        public MockMessageHandler(HttpResponseMessage expected)
        {
            _expected = expected;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await SendingAsync?.Invoke(request, cancellationToken);
            SentMessages++;
            return _expected;
        }
    }
}
