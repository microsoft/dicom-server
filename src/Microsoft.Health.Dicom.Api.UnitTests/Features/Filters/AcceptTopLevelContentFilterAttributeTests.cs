// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Filters
{
    public class AcceptTopLevelContentFilterAttributeTests
    {
        private readonly AcceptTopLevelContentFilterAttribute _filter;
        private readonly ActionExecutingContext _context;

        public AcceptTopLevelContentFilterAttributeTests()
        {
            _filter = CreateFilter("application/dicom+json", "application/dicom+xml");
            _context = CreateContext();
        }

        [Theory]
        [InlineData("application/dicom+json", null)]
        [InlineData("applicAtion/dICOM+Json", null)]
        [InlineData("multipart/related; type=\"application/dicom+json\"", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("multipart/related; type=\"blah\"", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("multipart/related;", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("application/dicom+json+something", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("application/dicom", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("application/xml", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("", (int)HttpStatusCode.NotAcceptable)]
        public void GivenARequestWithAValidAcceptHeader_WhenValidatingTheContentType_ThenCorrectStatusCodeShouldBeReturned(string acceptHeaderMediaType, int? expectedStatusCode)
        {
            _context.HttpContext.Request.Headers.Add("Accept", acceptHeaderMediaType);

            _filter.OnActionExecuting(_context);

            Assert.Equal(expectedStatusCode, (_context.Result as StatusCodeResult)?.StatusCode);
        }

        [Theory]
        [InlineData(null, "application/dicom+json", "image/jpg")]
        [InlineData((int)HttpStatusCode.NotAcceptable, "image/png", "image/jpg")]
        [InlineData((int)HttpStatusCode.NotAcceptable, "multipart/related; type=\"application/dicom+json\"", "image/jpg")]
        [InlineData(null, "multipart/related; type=\"image/jpg\"", "application/dicom+json")]
        public void GivenARequestWithMultipleAcceptHeaders_WhenValidatingTheContentType_ThenCorrectStatusCodeShouldBeReturned(int? expectedStatusCode, params string[] acceptHeaderMediaType)
        {
            _context.HttpContext.Request.Headers.Add("Accept", acceptHeaderMediaType);

            _filter.OnActionExecuting(_context);

            Assert.Equal(expectedStatusCode, (_context.Result as StatusCodeResult)?.StatusCode);
        }

        [Fact]
        public void GivenARequestWithNoAcceptHeader_WhenValidatingTheContentType_ThenNotAcceptableStatusCodeShouldBeReturned()
        {
            _filter.OnActionExecuting(_context);

            Assert.Equal((int)HttpStatusCode.NotAcceptable, (_context.Result as StatusCodeResult)?.StatusCode);
        }

        private AcceptTopLevelContentFilterAttribute CreateFilter(params string[] supportedMediaTypes)
        {
            return new AcceptTopLevelContentFilterAttribute(supportedMediaTypes);
        }

        private static ActionExecutingContext CreateContext()
        {
            return new ActionExecutingContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                null);
        }
    }
}
