// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Web;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Filters
{
    public class BodyModelStateValidatorAttributeTests
    {
        private readonly BodyModelStateValidatorAttribute _filter;
        private readonly ActionExecutingContext _context;

        public BodyModelStateValidatorAttributeTests()
        {
            _context = CreateContext();
            _filter = new BodyModelStateValidatorAttribute();
        }

        [Fact]
        public void GivenModelError_WhenOnActionExecution_ThenShouldThrowInvalidRequestBodyException()
        {
            string key1 = "key1";
            string key2 = "key2";
            string key3 = "key3";
            string error1 = "error1";
            string error2 = "error2";
            string error3 = "error3";
            _context.ModelState.SetModelValue(key1, new ValueProviderResult("world"));
            _context.ModelState.AddModelError(key2, error1);
            _context.ModelState.AddModelError(key2, error2);
            _context.ModelState.AddModelError(key3, error3);
            var exp = Assert.Throws<InvalidRequestBodyException>(() => _filter.OnActionExecuting(_context));
            Assert.Equal($"The request body is not valid: {key2} - {error1}", exp.Message);
        }

        private static ActionExecutingContext CreateContext()
        {
            return Substitute.For<ActionExecutingContext>(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                null);
        }
    }
}
