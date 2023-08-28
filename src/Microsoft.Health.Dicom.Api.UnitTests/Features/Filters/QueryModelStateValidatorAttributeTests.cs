// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Core.Exceptions;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Filters;

public class QueryModelStateValidatorAttributeTests
{
    private readonly QueryModelStateValidatorAttribute _validator;
    private readonly ActionExecutingContext _context;

    public QueryModelStateValidatorAttributeTests()
    {
        _context = CreateContext();
        _validator = new QueryModelStateValidatorAttribute();
        _context.ModelState.AddModelError("frames", "This Error Message Should Not be escaped");
    }

    [Fact]
    public void Givenvaliderrormessage_shouldnotbeescaped()
    {
        var ex = Assert.Throws<InvalidQueryStringValuesException>(() => _validator.OnActionExecuting(_context));
        Assert.Equal("The query parameter 'frames' is invalid. This Error Message Should Not be escaped", ex.Message);
    }

    [Fact]
    public void Giveninvvaliderrormessage_shouldbeescaped()
    {
        _context.ModelState.Clear();
        _context.ModelState.AddModelError("frames", "This Shoud be <> escaped");
        var ex = Assert.Throws<InvalidQueryStringValuesException>(() => _validator.OnActionExecuting(_context));
        Assert.Equal("The query parameter 'frames' is invalid. This Shoud be &lt;&gt; escaped", ex.Message);
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
