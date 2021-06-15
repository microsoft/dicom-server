// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Health.Dicom.Functions.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Extensions
{
    public class HttpRequestExtensionsTests
    {
        [Fact]
        public void CreateRequestAbortedLinkedTokenSource_GivenNullRequest_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => HttpRequestExtensions.CreateRequestAbortedLinkedTokenSource(null, CancellationToken.None));
        }

        [Fact]
        public void CreateRequestAbortedLinkedTokenSource_GivenValidInput_ReturnsLinkedCancellationTokenSource()
        {
            using var mockLifetimeFeature = new MockLifetimeFeature();
            var featureCollection = new FeatureCollection();
            featureCollection.Set<IHttpRequestLifetimeFeature>(mockLifetimeFeature);

            var context = new DefaultHttpContext(featureCollection);

            // Host Cancellation
            using (var upstreamSource = new CancellationTokenSource())
            using (CancellationTokenSource source = context.Request.CreateRequestAbortedLinkedTokenSource(upstreamSource.Token))
            {
                Assert.False(source.IsCancellationRequested);

                upstreamSource.Cancel();
                Assert.True(source.IsCancellationRequested);
            }

            // Connection aborted
            using (var upstreamSource = new CancellationTokenSource())
            using (CancellationTokenSource source = context.Request.CreateRequestAbortedLinkedTokenSource(upstreamSource.Token))
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
