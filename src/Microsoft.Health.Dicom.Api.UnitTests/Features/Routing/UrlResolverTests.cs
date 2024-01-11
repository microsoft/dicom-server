// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Operations;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Routing;

public class UrlResolverTests
{
    private const string DefaultScheme = "http";
    private const string DefaultHost = "test";

    private readonly IUrlHelperFactory _urlHelperFactory = Substitute.For<IUrlHelperFactory>();
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly IActionContextAccessor _actionContextAccessor = Substitute.For<IActionContextAccessor>();

    private readonly UrlResolver _urlResolver;

    private readonly IUrlHelper _urlHelper = Substitute.For<IUrlHelper>();
    private readonly DefaultHttpContext _httpContext = new DefaultHttpContext();
    private readonly ActionContext _actionContext = new ActionContext();
    private readonly LinkGenerator _linkGenerator = Substitute.For<LinkGenerator>();

    private UrlRouteContext _capturedUrlRouteContext;

    public UrlResolverTests()
    {
        _httpContext.Request.Scheme = DefaultScheme;
        _httpContext.Request.Host = new HostString(DefaultHost);

        _httpContextAccessor.HttpContext.Returns(_httpContext);

        _actionContextAccessor.ActionContext.Returns(_actionContext);

        _urlHelper.RouteUrl(
            Arg.Do<UrlRouteContext>(c => _capturedUrlRouteContext = c));

        _urlHelperFactory.GetUrlHelper(_actionContext).Returns(_urlHelper);

        _urlHelper.RouteUrl(Arg.Any<UrlRouteContext>()).Returns($"{DefaultScheme}://{DefaultHost}");

        _urlResolver = new UrlResolver(
               _urlHelperFactory,
               _httpContextAccessor,
               _actionContextAccessor,
               _linkGenerator);
    }

    [Theory]
    [InlineData("v1.0-prerelease")]
    [InlineData("v1")]
    public void GivenOperationId_WhenRetrieveOperationStatusUriIsResolved_ThenCorrectUrlShouldBeReturned(string version)
    {
        Guid operationId = Guid.NewGuid();

        _httpContext.Request.RouteValues.Add(KnownActionParameterNames.Version, version);

        _urlResolver.ResolveOperationStatusUri(operationId);

        ValidateUrlRouteContext(
            KnownRouteNames.OperationStatus,
            routeValues => Assert.Equal(operationId.ToString(OperationId.FormatSpecifier), routeValues[KnownActionParameterNames.OperationId]));
    }

    [Theory]
    [InlineData("v1.0-prerelease")]
    [InlineData("v1")]
    public void GivenAStudy_WhenRetrieveStudyUriIsResolved_ThenCorrectUrlShouldBeReturned(string version)
    {
        const string studyInstanceUid = "123.123";

        _httpContext.Request.RouteValues.Add(KnownActionParameterNames.Version, version);

        _urlResolver.ResolveRetrieveStudyUri(studyInstanceUid);

        ValidateUrlRouteContext(
            KnownRouteNames.RetrieveStudy,
            routeValues =>
            {
                Assert.Equal(studyInstanceUid, routeValues[KnownActionParameterNames.StudyInstanceUid]);
            });
    }

    [Theory]
    [InlineData("v1.0-prerelease")]
    [InlineData("v1")]
    public void GivenAStudy_WhenRetrieveStudyUriWithPartitionIdIsResolved_ThenCorrectUrlShouldBeReturned(string version)
    {
        const string studyInstanceUid = "123.123";
        const string partitionName = "partition1";
        _httpContext.Request.RouteValues.Add(KnownActionParameterNames.PartitionName, partitionName);
        _httpContext.Request.RouteValues.Add(KnownActionParameterNames.Version, version);

        _urlResolver.ResolveRetrieveStudyUri(studyInstanceUid);

        ValidateUrlRouteContext(
            KnownRouteNames.PartitionRetrieveStudy,
            routeValues =>
            {
                Assert.Equal(studyInstanceUid, routeValues[KnownActionParameterNames.StudyInstanceUid]);
                Assert.Equal(partitionName, routeValues[KnownActionParameterNames.PartitionName]);
            });
    }

    [Theory]
    [InlineData("v1.0-prerelease")]
    [InlineData("v1")]
    public void GivenAnInstance_WhenRetrieveInstanceUriIsResolved_ThenCorrectUrlShouldBeReturned(string version)
    {
        const string studyInstanceUid = "123.123";
        const string seriesInstanceUid = "456.456";
        const string sopInstanceUid = "789.789";

        _httpContext.Request.RouteValues.Add(KnownActionParameterNames.Version, version);

        var instance = new InstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid, Partition.Default);

        _urlResolver.ResolveRetrieveInstanceUri(instance, false);

        ValidateUrlRouteContext(
            KnownRouteNames.RetrieveInstance,
            routeValues =>
            {
                Assert.Equal(studyInstanceUid, routeValues[KnownActionParameterNames.StudyInstanceUid]);
                Assert.Equal(seriesInstanceUid, routeValues[KnownActionParameterNames.SeriesInstanceUid]);
                Assert.Equal(sopInstanceUid, routeValues[KnownActionParameterNames.SopInstanceUid]);
            });
    }

    [Theory]
    [InlineData("v1.0-prerelease")]
    [InlineData("v1")]
    public void GivenAnInstance_WhenResolveRetrieveWorkitemUriResolved_ThenCorrectUrlShouldBeReturned(string version)
    {
        const string workitemInstanceUid = "123.123";
        const string partitionName = "partition1";
        _httpContext.Request.RouteValues.Add(KnownActionParameterNames.PartitionName, partitionName);
        _httpContext.Request.RouteValues.Add(KnownActionParameterNames.Version, version);

        _urlResolver.ResolveRetrieveWorkitemUri(workitemInstanceUid);

        ValidateUrlRouteContext(
            KnownRouteNames.PartitionedRetrieveWorkitemInstance,
            routeValues =>
            {
                Assert.Equal(workitemInstanceUid, routeValues[KnownActionParameterNames.WorkItemInstanceUid]);
                Assert.Equal(partitionName, routeValues[KnownActionParameterNames.PartitionName]);
            });
    }

    private void ValidateUrlRouteContext(string routeName, Action<RouteValueDictionary> routeValuesValidator = null)
    {
        Assert.NotNull(_capturedUrlRouteContext);

        Assert.Equal(routeName, _capturedUrlRouteContext.RouteName);
        Assert.Equal(DefaultScheme, _capturedUrlRouteContext.Protocol);
        Assert.Equal(DefaultHost, _capturedUrlRouteContext.Host);

        RouteValueDictionary routeValues = Assert.IsType<RouteValueDictionary>(_capturedUrlRouteContext.Values);

        routeValuesValidator(routeValues);
    }
}
