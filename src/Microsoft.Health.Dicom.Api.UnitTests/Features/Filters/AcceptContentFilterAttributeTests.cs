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
    public class AcceptContentFilterAttributeTests
    {
        [Theory]
        [InlineData("application/dicom+json", null)]
        [InlineData("applicAtion/dICOM+Json", null)]
        [InlineData("application/dicom+json+something", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("application/dicom", (int)HttpStatusCode.NotAcceptable)]
        [InlineData("application/xml", (int)HttpStatusCode.NotAcceptable)]
        public void GivenARequestWithAValidFormatQuerystring_WhenValidatingTheContentType_ThenCorrectStatusCodeShouldBeReturned(string supportedMediaType, int? expectedStatusCode)
        {
            AcceptContentFilterAttribute filter = CreateFilter(supportedMediaType);
            ActionExecutingContext context = CreateContext();

            context.HttpContext.Request.Headers.Add("Accept", "application/dicom+json");

            filter.OnActionExecuting(context);

            Assert.Equal(expectedStatusCode, (context.Result as StatusCodeResult)?.StatusCode);
        }

        private AcceptContentFilterAttribute CreateFilter(params string[] supportedMediaTypes)
        {
            return new AcceptContentFilterAttribute(supportedMediaTypes);
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
