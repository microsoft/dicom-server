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
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Filters
{
    public class AcceptTransferSyntaxFilterAttributeTests
    {
        private const string DefaultTransferSyntax = "*";
        private const string TransferSyntaxHeaderPrefix = "transfer-syntax";
        private AcceptTransferSyntaxFilterAttribute _filter;
        private readonly ActionExecutingContext _context;

        public AcceptTransferSyntaxFilterAttributeTests()
        {
            _context = CreateContext();
        }

        [Theory]
        [InlineData("hello")]
        [InlineData("1.2.840.10008.1.2.4.50")]
        public void GivenARequestWithUnsupportedTransferSyntax_WhenValidatingTheTransferSyntaxAgainstTransferSyntaxFilter_ThenCorrectStatusCodeShouldBeReturned(string parsedTransferSyntax)
        {
            _filter = CreateFilter(new[] { DefaultTransferSyntax });
            _context.ModelState.SetModelValue(TransferSyntaxHeaderPrefix, new ValueProviderResult(parsedTransferSyntax));

            _filter.OnActionExecuting(_context);

            Assert.Equal((int)HttpStatusCode.NotAcceptable, (_context.Result as StatusCodeResult)?.StatusCode);
        }

        [Theory]
        [InlineData("*")]
        public void GivenARequestWithSupportedTransferSyntax_WhenValidatingTheTransferSyntaxAgainstTransferSyntaxFilter_ThenFilterShouldPass(string parsedTransferSyntax)
        {
            _filter = CreateFilter(new[] { DefaultTransferSyntax });
            _context.ModelState.SetModelValue(TransferSyntaxHeaderPrefix, new ValueProviderResult(parsedTransferSyntax));

            _filter.OnActionExecuting(_context);

            Assert.Null((_context.Result as StatusCodeResult)?.StatusCode);
        }

        [Fact]
        public void GivenARequestWithNoTransferSyntaxValue_WhenValidatingTheTransferSyntaxAgainstTransferSyntaxFilter_ThenNotAcceptableStatusCodeShouldBeReturned()
        {
            _filter = CreateFilter(new[] { DefaultTransferSyntax });
            _context.ModelState.SetModelValue(TransferSyntaxHeaderPrefix, null, null);

            _filter.OnActionExecuting(_context);

            Assert.Equal((int)HttpStatusCode.NotAcceptable, (_context.Result as StatusCodeResult)?.StatusCode);
        }

        [Fact]
        public void GivenARequestWithNoTransferSyntaxValue_WhenValidatingTheTransferSyntaxAgainstTransferSyntaxFilterWhichAllowMissing_ThenFilterShouldPass()
        {
            _filter = CreateFilter(new[] { DefaultTransferSyntax }, allowMissing: true);
            _context.ModelState.SetModelValue(TransferSyntaxHeaderPrefix, null, null);

            _filter.OnActionExecuting(_context);

            Assert.Null((_context.Result as StatusCodeResult)?.StatusCode);
        }

        [Fact]
        public void GivenARequestWithNoSetTransferSyntaxHeader_WhenValidatingTheTransferSyntaxAgainstTransferSyntaxFilter_ThenNotAcceptableStatusCodeShouldBeReturned()
        {
            _filter = CreateFilter(new[] { DefaultTransferSyntax });
            _filter.OnActionExecuting(_context);

            Assert.Equal((int)HttpStatusCode.NotAcceptable, (_context.Result as StatusCodeResult)?.StatusCode);
        }

        [Fact]
        public void GivenARequestWithNoSetTransferSyntaxHeader_WhenValidatingTheTransferSyntaxAgainstTransferSyntaxFilterWhichAllowMissing_ThenFilterShouldPass()
        {
            _filter = CreateFilter(new[] { DefaultTransferSyntax }, allowMissing: true);
            _filter.OnActionExecuting(_context);

            Assert.Null((_context.Result as StatusCodeResult)?.StatusCode);
        }

        private AcceptTransferSyntaxFilterAttribute CreateFilter(string[] supportedTransferSyntaxes, bool allowMissing = false)
        {
            return new AcceptTransferSyntaxFilterAttribute(supportedTransferSyntaxes, allowMissing);
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
