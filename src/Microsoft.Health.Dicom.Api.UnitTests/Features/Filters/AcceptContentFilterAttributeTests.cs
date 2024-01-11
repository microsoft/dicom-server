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
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Filters;

/// <summary>
/// These tests leverage the existing ASP.NET Core tests at: https://github.com/dotnet/aspnetcore/blob/main/src/Http/Headers/test/MediaTypeHeaderValueTest.cs
/// </summary>
public class AcceptContentFilterAttributeTests
{
    private AcceptContentFilterAttribute _filter;
    private readonly ActionExecutingContext _context;

    public AcceptContentFilterAttributeTests()
    {
        _context = CreateContext();
    }

    [Fact]
    public void GivenARequestWithNoAcceptHeader_ThenNotAcceptableStatusCodeShouldBeReturned()
    {
        _filter = CreateFilter([KnownContentTypes.ApplicationDicom, KnownContentTypes.ApplicationOctetStream]);

        _filter.OnActionExecuting(_context);

        Assert.Equal((int)HttpStatusCode.NotAcceptable, (_context.Result as StatusCodeResult)?.StatusCode);
    }

    [Theory]
    [InlineData("*/*")]
    [InlineData("application/*")]
    [InlineData("application/json")]
    [InlineData("application/xml")]
    [InlineData("application/dicom+json")]
    [InlineData("applicAtion/dICOM+Json")]
    [InlineData("application/dicom+xml")]
    [InlineData("applicAtion/DICOM+XmL")]
    [InlineData("application/dicom+json; transfer-syntax=*")]
    [InlineData("application/dicom+json; transfer-syntax=\"*\"")]
    public void GivenARequestWithAValidAcceptHeader_WhenMediaTypeMatches_ThenSuccess(string acceptHeaderMediaType)
    {
        _filter = CreateFilter([KnownContentTypes.ApplicationDicomJson, "application/dicom+xml"]);

        _context.HttpContext.Request.Headers.TryAdd(HeaderNames.Accept, acceptHeaderMediaType);

        _filter.OnActionExecuting(_context);

        Assert.Null((_context.Result as StatusCodeResult)?.StatusCode);
    }

    [Theory]
    [InlineData("application/dicom+json+something")]
    [InlineData("application/dicom")]
    [InlineData("")]
    [InlineData(null)]
    public void GivenARequestWithAValidAcceptHeader_WhenMediaTypeDoesntMatch_ThenFailure(string acceptHeaderMediaType)
    {
        _filter = CreateFilter([KnownContentTypes.ApplicationDicomJson, "application/dicom+xml"]);

        _context.HttpContext.Request.Headers.Append(HeaderNames.Accept, acceptHeaderMediaType);

        _filter.OnActionExecuting(_context);

        Assert.Equal((int)HttpStatusCode.NotAcceptable, (_context.Result as StatusCodeResult)?.StatusCode);
    }

    [Theory]
    [InlineData("application/dicom+json", "image/jpg")]
    [InlineData("application/dicom+xml", "image/png")]
    [InlineData("application/dicom+json", "application/dicom+xml")]
    [InlineData("image/png", "application/dicom+xml")]
    [InlineData("application/dicom", "application/xml")]
    public void GivenARequestWithMultipleAcceptHeaders_WhenAnyMediaTypeMatches_ThenSuccess(params string[] acceptHeaderMediaType)
    {
        _filter = CreateFilter([KnownContentTypes.ApplicationDicomJson, "application/dicom+xml"]);

        _context.HttpContext.Request.Headers.Append(HeaderNames.Accept, acceptHeaderMediaType);

        _filter.OnActionExecuting(_context);

        Assert.Null((_context.Result as StatusCodeResult)?.StatusCode);
    }

    [Theory]
    [InlineData("image/png", "image/jpg")]
    [InlineData("application/dicom", "image/png")]
    public void GivenARequestWithMultipleAcceptHeaders_WhenNoMediaTypeMatches_ThenFailure(params string[] acceptHeaderMediaType)
    {
        _filter = CreateFilter([KnownContentTypes.ApplicationDicomJson, "application/dicom+xml"]);

        _context.HttpContext.Request.Headers.Append(HeaderNames.Accept, acceptHeaderMediaType);

        _filter.OnActionExecuting(_context);

        Assert.Equal((int)HttpStatusCode.NotAcceptable, (_context.Result as StatusCodeResult)?.StatusCode);
    }

    [Theory]
    [InlineData("image/png, image/jpg, application/dicom+xml")]
    [InlineData("application/dicom+json, application/xml")]
    public void GivenARequestWithOneAcceptHeaderWithMultipleTypes_WhenAnyMediaTypeMatches_ThenSuccess(string acceptHeaderMediaType)
    {
        _filter = CreateFilter([KnownContentTypes.ApplicationDicomJson, "application/dicom+xml"]);

        _context.HttpContext.Request.Headers.Append(HeaderNames.Accept, acceptHeaderMediaType);

        _filter.OnActionExecuting(_context);

        Assert.Null((_context.Result as StatusCodeResult)?.StatusCode);
    }

    [Theory]
    [InlineData("image/png, image/jpg, application/dicom")]
    [InlineData("application/dicom, application/pdf")]
    public void GivenARequestWithOneAcceptHeaderWithMultipleTypes_WhenNoMediaTypeMatches_ThenFailure(string acceptHeaderMediaType)
    {
        _filter = CreateFilter([KnownContentTypes.ApplicationDicomJson, "application/dicom+xml"]);

        _context.HttpContext.Request.Headers.Append(HeaderNames.Accept, acceptHeaderMediaType);

        _filter.OnActionExecuting(_context);

        Assert.Equal((int)HttpStatusCode.NotAcceptable, (_context.Result as StatusCodeResult)?.StatusCode);
    }

    private static AcceptContentFilterAttribute CreateFilter(string[] supportedMediaTypes)
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
