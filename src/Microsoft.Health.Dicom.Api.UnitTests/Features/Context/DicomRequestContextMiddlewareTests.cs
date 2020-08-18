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

        private async Task<IRequestContext> SetupAsync(HttpContext httpContext)
        {
            var dicomRequestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();
            var dicomContextMiddlware = new DicomRequestContextMiddleware(next: (innerHttpContext) => Task.CompletedTask);

            await dicomContextMiddlware.Invoke(httpContext, dicomRequestContextAccessor);

            Assert.NotNull(dicomRequestContextAccessor.DicomRequestContext);

            return dicomRequestContextAccessor.DicomRequestContext;
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
