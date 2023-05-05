// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Net.Http.Headers;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Filters;

public class PopulateOriginalRequestFilterAttributeTests
{
    private readonly ActionExecutingContext _actionExecutingContext;
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
    private readonly IOptions<FeatureConfiguration> _featureConfiguration;
    private readonly ActionExecutionDelegate _nextActionDelegate;
    private PopulateOriginalRequestFilterAttribute _filterAttribute;

    public PopulateOriginalRequestFilterAttributeTests()
    {
        _actionExecutingContext = CreateContext();
        _dicomRequestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();
        _featureConfiguration = Options.Create(new FeatureConfiguration { EnableUpdate = true });
        _nextActionDelegate = Substitute.For<ActionExecutionDelegate>();
    }

    [Fact]
    public async Task GivenRequestWithUpdateEnabled_WhenNoAcceptHeader_ThenItExecutesSuccessfully()
    {
        _filterAttribute = CreateFilter(_dicomRequestContextAccessor, _featureConfiguration);
        _actionExecutingContext.HttpContext.Request.Headers.TryAdd(HeaderNames.Accept, "application/dicom");
        await _filterAttribute.OnActionExecutionAsync(_actionExecutingContext, _nextActionDelegate);

        Assert.False(_dicomRequestContextAccessor.RequestContext.IsOriginalRequested);
    }

    [Fact]
    public async Task GivenRequestWithUpdateEnabled_WithAcceptHeader_ThenItExecutesSuccessfully()
    {
        _featureConfiguration.Value.EnableUpdate = true;
        _filterAttribute = CreateFilter(_dicomRequestContextAccessor, _featureConfiguration);
        _actionExecutingContext.HttpContext.Request.Headers.TryAdd(HeaderNames.Accept, "application/dicom;msdicom-request-original");

        await _filterAttribute.OnActionExecutionAsync(_actionExecutingContext, _nextActionDelegate);

        _dicomRequestContextAccessor
            .Received()
            .RequestContext
            .IsOriginalRequested = true;
    }

    [Fact]
    public async Task GivenRequestWithUpdateDisabled_WithoutAcceptHeader_ThenItExecutesSuccessfully()
    {
        _featureConfiguration.Value.EnableUpdate = false;
        _filterAttribute = CreateFilter(_dicomRequestContextAccessor, _featureConfiguration);
        _actionExecutingContext.HttpContext.Request.Headers.TryAdd(HeaderNames.Accept, "application/dicom;");

        await _filterAttribute.OnActionExecutionAsync(_actionExecutingContext, _nextActionDelegate);

        Assert.False(_dicomRequestContextAccessor.RequestContext.IsOriginalRequested);
    }

    [Fact]
    public void GivenRequestWithUpdateDisabled_WhenAcceptHeaderIsPassed_ThenItThrows()
    {
        _featureConfiguration.Value.EnableUpdate = false;
        _filterAttribute = CreateFilter(_dicomRequestContextAccessor, _featureConfiguration);

        _actionExecutingContext.HttpContext.Request.Headers.TryAdd(HeaderNames.Accept, "application/dicom");
        _actionExecutingContext.HttpContext.Request.Headers.TryAdd("msdicom-request-original", "true");
        Assert.ThrowsAsync<DicomUpdateFeatureDisabledException>(async () => await _filterAttribute.OnActionExecutionAsync(_actionExecutingContext, _nextActionDelegate));
    }

    private static PopulateOriginalRequestFilterAttribute CreateFilter(IDicomRequestContextAccessor dicomRequestContextAccessor, IOptions<FeatureConfiguration> featureConfiguration)
    {
        return new PopulateOriginalRequestFilterAttribute(dicomRequestContextAccessor, featureConfiguration);
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
