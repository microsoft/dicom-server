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
    public class AcceptContentFilterAttributeTests
    {
        private AcceptContentFilterAttribute _filter;
        private readonly ActionExecutingContext _context;

        public AcceptContentFilterAttributeTests()
        {
            _context = CreateContext();
        }

        [Theory]
        [InlineData("application/dicom+json", null)]
        [InlineData("applicAtion/dICOM+Json", null)]
        [InlineData("multipart/related; type=\"application/dicom+json\"", null)]
        [InlineData("multipart/related; type=\"application/dicom\"", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("multipart/related; type=\"blah\"", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("multipart/related;", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("application/dicom+json+something", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("application/dicom", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("application/xml", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("", (int)HttpStatusCode.NotAcceptable)]
        public void GivenARequestWithAValidAcceptHeader_WhenValidatingTheContentTypeAgainstSingleAndMultipartHeaderFilter_ThenCorrectStatusCodeShouldBeReturned(string acceptHeaderMediaType, int? expectedStatusCode)
        {
            _filter = CreateFilter(new[] { KnownContentTypes.ApplicationDicomJson, "application/dicom+xml" }, true, true);

            _context.HttpContext.Request.Headers.Add("Accept", acceptHeaderMediaType);

            _filter.OnActionExecuting(_context);

            Assert.Equal(expectedStatusCode, (_context.Result as StatusCodeResult)?.StatusCode);
        }

        [Theory]
        [InlineData(null, "application/dicom+json", "image/jpg")]
        [InlineData((int)HttpStatusCode.NotAcceptable, "image/png", "image/jpg")]
        [InlineData(null, "multipart/related; type=\"application/dicom+json\"", "image/jpg")]
        [InlineData(null, "multipart/related; type=\"application/dicom+json\"", "multipart/related; type=\"image/jpg\"")]
        [InlineData((int)HttpStatusCode.NotAcceptable, "multipart/related; type=\"image/png\"", "multipart/related; type=\"image/jpg\"")]
        [InlineData(null, "multipart/related; type=\"image/jpg\"", "application/dicom+json")]
        public void GivenARequestWithMultipleAcceptHeaders_WhenValidatingTheContentTypeAgainstSingleAndMultipartHeaderFilter_ThenCorrectStatusCodeShouldBeReturned(int? expectedStatusCode, params string[] acceptHeaderMediaType)
        {
            _filter = CreateFilter(new[] { KnownContentTypes.ApplicationDicomJson, "application/dicom+xml" }, true, true);

            _context.HttpContext.Request.Headers.Add("Accept", acceptHeaderMediaType);

            _filter.OnActionExecuting(_context);

            Assert.Equal(expectedStatusCode, (_context.Result as StatusCodeResult)?.StatusCode);
        }

        [Fact]
        public void GivenARequestWithNoAcceptHeader_WhenValidatingTheContentTypeAgainstSingleAndMultipartHeaderFilter_ThenNotAcceptableStatusCodeShouldBeReturned()
        {
            _filter = CreateFilter(new[] { KnownContentTypes.ApplicationDicomJson, "application/dicom+xml" }, true, true);

            _filter.OnActionExecuting(_context);

            Assert.Equal((int)HttpStatusCode.NotAcceptable, (_context.Result as StatusCodeResult)?.StatusCode);
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
        public void GivenARequestWithAValidAcceptHeader_WhenValidatingTheContentTypeAgainstSingleHeaderFilter_ThenCorrectStatusCodeShouldBeReturned(string acceptHeaderMediaType, int? expectedStatusCode)
        {
            _filter = CreateFilter(new[] { KnownContentTypes.ApplicationDicomJson, "application/dicom+xml" }, true, false);

            _context.HttpContext.Request.Headers.Add("Accept", acceptHeaderMediaType);

            _filter.OnActionExecuting(_context);

            Assert.Equal(expectedStatusCode, (_context.Result as StatusCodeResult)?.StatusCode);
        }

        [Theory]
        [InlineData(null, "application/dicom+json", "image/jpg")]
        [InlineData((int)HttpStatusCode.NotAcceptable, "image/png", "image/jpg")]
        [InlineData((int)HttpStatusCode.NotAcceptable, "multipart/related; type=\"application/dicom+json\"", "image/jpg")]
        [InlineData(null, "multipart/related; type=\"image/jpg\"", "application/dicom+json")]
        public void GivenARequestWithMultipleAcceptHeaders_WhenValidatingTheContentTypeAgainstSingleHeaderFilter_ThenCorrectStatusCodeShouldBeReturned(int? expectedStatusCode, params string[] acceptHeaderMediaType)
        {
            _filter = CreateFilter(new[] { KnownContentTypes.ApplicationDicomJson, "application/dicom+xml" }, true, false);

            _context.HttpContext.Request.Headers.Add("Accept", acceptHeaderMediaType);

            _filter.OnActionExecuting(_context);

            Assert.Equal(expectedStatusCode, (_context.Result as StatusCodeResult)?.StatusCode);
        }

        [Fact]
        public void GivenARequestWithNoAcceptHeader_WhenValidatingTheContentTypeAgainstSingleHeaderFilter_ThenNotAcceptableStatusCodeShouldBeReturned()
        {
            _filter = CreateFilter(new[] { KnownContentTypes.ApplicationDicomJson, "application/dicom+xml" }, true, false);

            _filter.OnActionExecuting(_context);

            Assert.Equal((int)HttpStatusCode.NotAcceptable, (_context.Result as StatusCodeResult)?.StatusCode);
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
        public void GivenARequestWithAValidAcceptHeader_WhenValidatingTheContentTypeAgainstMultipartHeaderFilter_ThenCorrectStatusCodeShouldBeReturned(string acceptHeaderMediaType, int? expectedStatusCode)
        {
            _filter = CreateFilter(new[] { KnownContentTypes.ApplicationDicom, KnownContentTypes.ApplicationOctetStream }, false, true);

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
        public void GivenARequestWithMultipleAcceptHeaders_WhenValidatingTheContentTypeAgainstMultipartHeaderFilter_ThenCorrectStatusCodeShouldBeReturned(int? expectedStatusCode, params string[] acceptHeaderMediaType)
        {
            _filter = CreateFilter(new[] { KnownContentTypes.ApplicationDicom, KnownContentTypes.ApplicationOctetStream }, false, true);

            _context.HttpContext.Request.Headers.Add("Accept", acceptHeaderMediaType);

            _filter.OnActionExecuting(_context);

            Assert.Equal(expectedStatusCode, (_context.Result as StatusCodeResult)?.StatusCode);
        }

        [Fact]
        public void GivenARequestWithNoAcceptHeader_WhenValidatingTheContentTypeAgainstMultipartHeaderFilter_ThenNotAcceptableStatusCodeShouldBeReturned()
        {
            _filter = CreateFilter(new[] { KnownContentTypes.ApplicationDicom, KnownContentTypes.ApplicationOctetStream }, false, true);

            _filter.OnActionExecuting(_context);

            Assert.Equal((int)HttpStatusCode.NotAcceptable, (_context.Result as StatusCodeResult)?.StatusCode);
        }

        private AcceptContentFilterAttribute CreateFilter(string[] supportedMediaTypes, bool allowSingle, bool allowMultiple)
        {
            return new AcceptContentFilterAttribute(supportedMediaTypes, allowSingle, allowMultiple);
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
