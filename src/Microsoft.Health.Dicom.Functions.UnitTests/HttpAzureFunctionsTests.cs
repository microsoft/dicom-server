// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests
{
    public class HttpAzureFunctionsTests
    {
        [Fact]
        public void CreateCancellationSource_GivenNullRequest_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => HttpAzureFunctions.CreateCancellationSource(null, CancellationToken.None));
        }

        [Fact]
        public void CreateCancellationSource_GivenValidInput_ReturnsLinkedCancellationTokenSource()
        {
            using var mockLifetimeFeature = new MockLifetimeFeature();
            var featureCollection = new FeatureCollection();
            featureCollection.Set<IHttpRequestLifetimeFeature>(mockLifetimeFeature);

            var context = new DefaultHttpContext(featureCollection);

            // Host Cancellation
            using (var hostSource = new CancellationTokenSource())
            using (CancellationTokenSource source = HttpAzureFunctions.CreateCancellationSource(context.Request, hostSource.Token))
            {
                Assert.False(source.IsCancellationRequested);

                hostSource.Cancel();
                Assert.True(source.IsCancellationRequested);
            }

            // Connection aborted
            using (var hostSource = new CancellationTokenSource())
            using (CancellationTokenSource source = HttpAzureFunctions.CreateCancellationSource(context.Request, hostSource.Token))
            {
                Assert.False(source.IsCancellationRequested);

                context.Abort();
                Assert.True(source.IsCancellationRequested);
            }
        }

        private sealed class MockLifetimeFeature : IHttpRequestLifetimeFeature, IDisposable
        {
            public CancellationToken RequestAborted
            {
                get => _cancellationTokenSource.Token;
                set => throw new NotSupportedException();
            }

            private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

            public void Abort()
                => _cancellationTokenSource.Cancel();

            public void Dispose()
            {
                _cancellationTokenSource.Dispose();
            }
        }
    }
}
