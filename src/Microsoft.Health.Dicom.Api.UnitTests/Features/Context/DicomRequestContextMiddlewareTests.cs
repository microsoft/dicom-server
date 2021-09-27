// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Core.Features.Context;
using Microsoft.Health.Dicom.Api.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Context;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Context
{
    public class DicomRequestContextMiddlewareTests
    {
        [Fact]
        public async Task GivenAnHttpRequest_WhenExecutingDicomRequestContextMiddleware_ThenCorrectUriShouldBeSet()
        {
            IRequestContext dicomRequestContext = await SetupAsync(CreateHttpContext());

            Assert.Equal(new Uri("https://localhost:30/studies/123"), dicomRequestContext.Uri);
        }

        [Fact]
        public async Task GivenAnHttpRequest_WhenExecutingDicomRequestContextMiddleware_ThenCorrectBaseUriShouldBeSet()
        {
            IRequestContext dicomRequestContext = await SetupAsync(CreateHttpContext());

            Assert.Equal(new Uri("https://localhost:30/studies"), dicomRequestContext.BaseUri);
        }

        [Fact]
        public async Task GivenAnHttpRequest_WhenExecutingDicomRequestContextMiddleware_ThenResponseSizeShouldBeSet()
        {
            long largeRequestLength = 2000000000; // 2gb
            long headerSize = 2;
            IDicomRequestContext dicomRequestContext = await SetupAsync(CreateHttpContext(), largeRequestLength);

            Assert.Equal(largeRequestLength + headerSize, dicomRequestContext.ResponseSize);
        }

        private async Task<IDicomRequestContext> SetupAsync(HttpContext httpContext, long byteLength = 256)
        {
            var dicomRequestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();
            var dicomContextMiddleware = new DicomRequestContextMiddleware(next: (innerHttpContext) =>
            {
                innerHttpContext.Response.Body.Write(new byte[byteLength]);
                return Task.CompletedTask;
            });

            await dicomContextMiddleware.Invoke(httpContext, dicomRequestContextAccessor);

            Assert.NotNull(dicomRequestContextAccessor.RequestContext);

            return dicomRequestContextAccessor.RequestContext;
        }

        private HttpContext CreateHttpContext()
        {
            HttpContext httpContext = new DefaultHttpContext();

            httpContext.Request.Method = "GET";
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("localhost", 30);
            httpContext.Request.PathBase = new PathString("/studies");
            httpContext.Request.Path = new PathString("/123");

            return httpContext;
        }

    }
}
