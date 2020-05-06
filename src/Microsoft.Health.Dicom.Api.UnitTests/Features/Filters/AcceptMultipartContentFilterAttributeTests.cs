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
using Microsoft.Health.Dicom.Core.Web;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Filters
{
    public class AcceptMultipartContentFilterAttributeTests
    {
        private readonly AcceptMultipartContentFilterAttribute _filter;
        private readonly ActionExecutingContext _context;

        public AcceptMultipartContentFilterAttributeTests()
        {
            _filter = CreateFilter(KnownContentTypes.ApplicationDicom, KnownContentTypes.ApplicationOctetStream);
            _context = CreateContext();
        }

        [Theory]
        [InlineData("application/dicom", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("applicAtion/dICOM", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("multipart/related; type=\"application/dicom+json\"", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("multipart/related; type=\"application/octet_stream\"", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("multipart/related; type=\"application/octetStream\"", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("multipart/related; type=\"application/dicom\"", null)]
        [InlineData("multipart/relATed; type=\"applICation/DICom\"", null)]
        [InlineData("multipart/related; type=\"application/octet-stream\"", null)]
        [InlineData("multipart/related; type=\"blah\"", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("multipart/related;", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("application/dicom+something", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("application/dic", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("application/xml", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("", (int)HttpStatusCode.NotAcceptable)]
        public void GivenARequestWithAValidAcceptHeader_WhenValidatingTheContentType_ThenCorrectStatusCodeShouldBeReturned(string acceptHeaderMediaType, int? expectedStatusCode)
        {
            _context.HttpContext.Request.Headers.Add("Accept", acceptHeaderMediaType);

            _filter.OnActionExecuting(_context);

            Assert.Equal(expectedStatusCode, (_context.Result as StatusCodeResult)?.StatusCode);
        }

        [Theory]
        [InlineData((int)HttpStatusCode.NotAcceptable, "image/png", "image/jpg")]
        [InlineData(null, "multipart/related; type=\"application/dicom\"", "image/jpg")]
        [InlineData(null, "multipart/related; type=\"application/dicom\"", "multipart/related; type=\"image/jpg\"")]
        [InlineData((int)HttpStatusCode.NotAcceptable, "multipart/related; type=\"image/png\"", "multipart/related; type=\"image/jpg\"")]
        [InlineData((int)HttpStatusCode.NotAcceptable, "multipart/related; type=\"image/jpg\"", "application/dicom+json")]
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

        private AcceptMultipartContentFilterAttribute CreateFilter(params string[] supportedMediaTypes)
        {
            return new AcceptMultipartContentFilterAttribute(supportedMediaTypes);
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
