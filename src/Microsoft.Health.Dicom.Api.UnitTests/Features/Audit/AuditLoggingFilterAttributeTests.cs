// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Health.Dicom.Api.UnitTests.Features.Filters;
using Xunit;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Audit;

public class AuditLoggingFilterAttributeTests
{
    private readonly DicomAudit.AuditLoggingFilterAttribute _filter;

    private readonly HttpContext _httpContext = new DefaultHttpContext();

    public AuditLoggingFilterAttributeTests()
    {
        _filter = new DicomAudit.AuditLoggingFilterAttribute();
    }

    [Fact]
    public void GivenChangeFeedController_WhenExecutingAction_ThenAuditLogShouldBeLogged()
    {
        var actionExecutingContext = new ActionExecutingContext(
            new ActionContext(_httpContext, new RouteData(),
                new ControllerActionDescriptor() { DisplayName = "Executing ChangeFeed." }),
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            FilterTestsHelper.CreateMockChangeFeedController());

        _filter.OnActionExecuting(actionExecutingContext);
    }

    [Fact]
    public void GivenChangeFeedController_WhenExecutedActionThrowsException_ThenAuditLogShouldNotBeLogged()
    {
        var result = new NoContentResult();

        var actionExecutedContext = new ActionExecutedContext(
            new ActionContext(_httpContext, new RouteData(),
                new ControllerActionDescriptor() { DisplayName = "Executed ChangeFeed." }),
            new List<IFilterMetadata>(),
            FilterTestsHelper.CreateMockChangeFeedController());

        actionExecutedContext.Exception = new Exception("Test Exception.");

        _filter.OnActionExecuted(actionExecutedContext);
    }

    [Fact]
    public void GivenChangeFeedController_WhenExecutedAction_ThenAuditLogShouldNotBeLogged()
    {
        var result = new NoContentResult();

        var resultExecutedContext = new ResultExecutedContext(
            new ActionContext(_httpContext, new RouteData(),
                new ControllerActionDescriptor() { DisplayName = "Executed ChangeFeed." }),
            new List<IFilterMetadata>(),
            result,
            FilterTestsHelper.CreateMockChangeFeedController());

        _filter.OnResultExecuted(resultExecutedContext);
    }

    [Fact]
    public void GivenDeleteController_WhenExecutingAction_ThenAuditLogShouldBeLogged()
    {
        var actionExecutingContext = new ActionExecutingContext(
            new ActionContext(_httpContext, new RouteData(),
                new ControllerActionDescriptor() { DisplayName = "Executing Delete." }),
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            FilterTestsHelper.CreateMockDeleteController());

        _filter.OnActionExecuting(actionExecutingContext);
    }

    [Fact]
    public void GivenDeleteController_WhenExecutedActionThrowsException_ThenAuditLogShouldNotBeLogged()
    {
        var result = new NoContentResult();

        var actionExecutedContext = new ActionExecutedContext(
            new ActionContext(_httpContext, new RouteData(),
                new ControllerActionDescriptor() { DisplayName = "Executed Delete." }),
            new List<IFilterMetadata>(),
            FilterTestsHelper.CreateMockDeleteController());

        actionExecutedContext.Exception = new Exception("Test Exception.");

        _filter.OnActionExecuted(actionExecutedContext);
    }

    [Fact]
    public void GivenDeleteController_WhenExecutedAction_ThenAuditLogShouldNotBeLogged()
    {
        var result = new NoContentResult();

        var resultExecutedContext = new ResultExecutedContext(
            new ActionContext(_httpContext, new RouteData(),
                new ControllerActionDescriptor() { DisplayName = "Executed Delete." }),
            new List<IFilterMetadata>(),
            result,
            FilterTestsHelper.CreateMockDeleteController());

        _filter.OnResultExecuted(resultExecutedContext);
    }

    [Fact]
    public void GivenQueryController_WhenExecutingAction_ThenAuditLogShouldBeLogged()
    {
        var actionExecutingContext = new ActionExecutingContext(
            new ActionContext(_httpContext, new RouteData(),
                new ControllerActionDescriptor() { DisplayName = "Executing Query." }),
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            FilterTestsHelper.CreateMockQueryController());

        _filter.OnActionExecuting(actionExecutingContext);
    }

    [Fact]
    public void GivenQueryController_WhenExecutedActionThrowsException_ThenAuditLogShouldNotBeLogged()
    {
        var result = new NoContentResult();

        var actionExecutedContext = new ActionExecutedContext(
            new ActionContext(_httpContext, new RouteData(),
                new ControllerActionDescriptor() { DisplayName = "Executed Query." }),
            new List<IFilterMetadata>(),
            FilterTestsHelper.CreateMockQueryController());

        actionExecutedContext.Exception = new Exception("Test Exception.");

        _filter.OnActionExecuted(actionExecutedContext);
    }

    [Fact]
    public void GivenQueryController_WhenExecutedAction_ThenAuditLogShouldNotBeLogged()
    {
        var result = new NoContentResult();

        var resultExecutedContext = new ResultExecutedContext(
            new ActionContext(_httpContext, new RouteData(),
                new ControllerActionDescriptor() { DisplayName = "Executed Query." }),
            new List<IFilterMetadata>(),
            result,
            FilterTestsHelper.CreateMockQueryController());

        _filter.OnResultExecuted(resultExecutedContext);
    }

    [Fact]
    public void GivenRetrieveController_WhenExecutingAction_ThenAuditLogShouldBeLogged()
    {
        var actionExecutingContext = new ActionExecutingContext(
            new ActionContext(_httpContext, new RouteData(),
                new ControllerActionDescriptor() { DisplayName = "Executing Retrieve." }),
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            FilterTestsHelper.CreateMockRetrieveController());

        _filter.OnActionExecuting(actionExecutingContext);
    }

    [Fact]
    public void GivenRetrieveController_WhenExecutedActionThrowsException_ThenAuditLogShouldNotBeLogged()
    {
        var result = new NoContentResult();

        var actionExecutedContext = new ActionExecutedContext(
            new ActionContext(_httpContext, new RouteData(),
                new ControllerActionDescriptor() { DisplayName = "Executed Retrieve." }),
            new List<IFilterMetadata>(),
            FilterTestsHelper.CreateMockRetrieveController());

        actionExecutedContext.Exception = new Exception("Test Exception.");

        _filter.OnActionExecuted(actionExecutedContext);
    }

    [Fact]
    public void GivenRetrieveController_WhenExecutedAction_ThenAuditLogShouldNotBeLogged()
    {
        var result = new NoContentResult();

        var resultExecutedContext = new ResultExecutedContext(
            new ActionContext(_httpContext, new RouteData(),
                new ControllerActionDescriptor() { DisplayName = "Executed Retrieve." }),
            new List<IFilterMetadata>(),
            result,
            FilterTestsHelper.CreateMockRetrieveController());

        _filter.OnResultExecuted(resultExecutedContext);
    }

    [Fact]
    public void GivenStoreController_WhenExecutingAction_ThenAuditLogShouldBeLogged()
    {
        var actionExecutingContext = new ActionExecutingContext(
            new ActionContext(_httpContext, new RouteData(),
                new ControllerActionDescriptor() { DisplayName = "Executing Store." }),
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            FilterTestsHelper.CreateMockStoreController());

        _filter.OnActionExecuting(actionExecutingContext);
    }

    [Fact]
    public void GivenStoreController_WhenExecutedActionThrowsException_ThenAuditLogShouldNotBeLogged()
    {
        var result = new NoContentResult();

        var actionExecutedContext = new ActionExecutedContext(
            new ActionContext(_httpContext, new RouteData(),
                new ControllerActionDescriptor() { DisplayName = "Executed Store." }),
            new List<IFilterMetadata>(),
            FilterTestsHelper.CreateMockStoreController());

        actionExecutedContext.Exception = new Exception("Test Exception.");

        _filter.OnActionExecuted(actionExecutedContext);
    }

    [Fact]
    public void GivenStoreController_WhenExecutedAction_ThenAuditLogShouldNotBeLogged()
    {
        var result = new NoContentResult();

        var resultExecutedContext = new ResultExecutedContext(
            new ActionContext(_httpContext, new RouteData(),
                new ControllerActionDescriptor() { DisplayName = "Executed Store." }),
            new List<IFilterMetadata>(),
            result,
            FilterTestsHelper.CreateMockStoreController());

        _filter.OnResultExecuted(resultExecutedContext);
    }
}
