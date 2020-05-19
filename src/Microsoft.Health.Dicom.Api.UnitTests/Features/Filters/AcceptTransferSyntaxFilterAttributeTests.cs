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
    public class AcceptTransferSyntaxFilterAttributeTests
    {
        private const string DefaultTransferSyntax = "*";
        private AcceptTransferSyntaxFilterAttribute _filter;
        private readonly ActionExecutingContext _context;

        public AcceptTransferSyntaxFilterAttributeTests()
        {
            _context = CreateContext();
            _filter = CreateFilter(new[] { DefaultTransferSyntax });
        }

        [Theory]
        [InlineData("image/png;TransFer-syNtax=*\"")]
        [InlineData("image/png;TransFer-syNtax=hello")]
        [InlineData("image/png;TransFer-syNtax=1.2.840.10008.1.2.4.50")]
        [InlineData("image/png;TransFer-syNtax=1.2.840.10008.1.2.4.50", "image/png;TransFer-syNtax=hello")]
        [InlineData("image/png;TransFer-syNtax=1.2.840.10008.1.2.4.50", "  image/png;TransFer-syNtax   =      hello  ")]
        public void GivenARequestWithUnsupportedTransferSyntax_WhenValidatingTheTransferSyntaxAgainstTransferSyntaxFilter_ThenCorrectStatusCodeShouldBeReturned(params string[] acceptHeaders)
        {
            _context.HttpContext.Request.Headers.Add("Accept", acceptHeaders);

            _filter.OnActionExecuting(_context);

            Assert.Equal((int)HttpStatusCode.NotAcceptable, (_context.Result as StatusCodeResult)?.StatusCode);
        }

        [Theory]
        [InlineData("image/png;transfer-syntax=*")]
        [InlineData("image/png;transfer-syntax=*", "application/dicom")]
        [InlineData("image/png;   transfer-syntax  =   *    ", "application/dicom")]
        [InlineData(null)]
        public void GivenARequestWithSupportedTransferSyntax_WhenValidatingTheTransferSyntaxAgainstTransferSyntaxFilter_ThenFilterShouldPass(params string[] acceptHeaders)
        {
            _context.HttpContext.Request.Headers.Add("Accept", acceptHeaders);

            _filter.OnActionExecuting(_context);

            Assert.Null((_context.Result as StatusCodeResult)?.StatusCode);
        }

        [Theory]
        [InlineData("image/png;")]
        [InlineData("image/png;TransFersyNtax=*")]
        [InlineData("image/png; TransFersyNtax=*")]
        [InlineData("image/png; TransFer     syNtax=*")]
        [InlineData("image/png; TransFersyNtax=*", "application/dicom")]
        [InlineData(null)]
        public void GivenARequestWithNoTransferSyntaxValue_WhenValidatingTheTransferSyntaxAgainstTransferSyntaxFilter_ThenFilterShouldPass(params string[] acceptHeaders)
        {
            _context.HttpContext.Request.Headers.Add("Accept", acceptHeaders);

            _filter.OnActionExecuting(_context);

            Assert.Null((_context.Result as StatusCodeResult)?.StatusCode);
        }

        [Fact]
        public void GivenARequestWithNoAcceptHeader_WhenValidatingTheContentTypeAgainstMultipartHeaderFilter_ThenNotAcceptableStatusCodeShouldBeReturned()
        {
            _filter.OnActionExecuting(_context);

            Assert.Null((_context.Result as StatusCodeResult)?.StatusCode);
        }

        private AcceptTransferSyntaxFilterAttribute CreateFilter(string[] supportedTransferSyntaxes)
        {
            return new AcceptTransferSyntaxFilterAttribute(supportedTransferSyntaxes);
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
