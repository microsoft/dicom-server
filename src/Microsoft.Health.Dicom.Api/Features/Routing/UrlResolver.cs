// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Api.Features.Routing;

public sealed class UrlResolver : IUrlResolver
{
    private readonly IUrlHelperFactory _urlHelperFactory;

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IActionContextAccessor _actionContextAccessor;
    private readonly LinkGenerator _linkGenerator;

    public UrlResolver(
        IUrlHelperFactory urlHelperFactory,
        IHttpContextAccessor httpContextAccessor,
        IActionContextAccessor actionContextAccessor,
        LinkGenerator linkGenerator)
    {
        EnsureArg.IsNotNull(urlHelperFactory, nameof(urlHelperFactory));
        EnsureArg.IsNotNull(httpContextAccessor, nameof(httpContextAccessor));
        EnsureArg.IsNotNull(actionContextAccessor, nameof(actionContextAccessor));

        _urlHelperFactory = urlHelperFactory;
        _httpContextAccessor = httpContextAccessor;
        _actionContextAccessor = actionContextAccessor;
        _linkGenerator = linkGenerator;
    }

    private ActionContext ActionContext => _actionContextAccessor.ActionContext;

    private IUrlHelper UrlHelper => _urlHelperFactory.GetUrlHelper(ActionContext);

    /// <inheritdoc />
    public Uri ResolveOperationStatusUri(Guid operationId)
    {
        return RouteUri(
            KnownRouteNames.OperationStatus,
            new RouteValueDictionary
            {
                { KnownActionParameterNames.OperationId, operationId.ToString(OperationId.FormatSpecifier) },
            });
    }

    /// <inheritdoc />
    public Uri ResolveQueryTagUri(string tagPath)
    {
        return RouteUri(
            KnownRouteNames.GetExtendedQueryTag,
            new RouteValueDictionary
            {
                { KnownActionParameterNames.TagPath, tagPath },
            });
    }

    /// <inheritdoc />
    public Uri ResolveQueryTagErrorsUri(string tagPath)
    {
        return RouteUri(
            KnownRouteNames.GetExtendedQueryTagErrors,
            new RouteValueDictionary
            {
                { KnownActionParameterNames.TagPath, tagPath },
            });
    }

    /// <inheritdoc />
    public Uri ResolveRetrieveStudyUri(string studyInstanceUid)
    {
        EnsureArg.IsNotNull(studyInstanceUid, nameof(studyInstanceUid));
        var routeValues = new RouteValueDictionary
        {
            { KnownActionParameterNames.StudyInstanceUid, studyInstanceUid },
        };

        AddRouteValues(routeValues, out bool hasPartition);

        var routeName = hasPartition
            ? KnownRouteNames.PartitionRetrieveStudy
            : KnownRouteNames.RetrieveStudy;

        return RouteUri(routeName, routeValues);
    }

    /// <inheritdoc />
    public Uri ResolveRetrieveWorkitemUri(string workitemInstanceUid)
    {
        EnsureArg.IsNotNull(workitemInstanceUid, nameof(workitemInstanceUid));
        var routeValues = new RouteValueDictionary
        {
            { KnownActionParameterNames.WorkItemInstanceUid, workitemInstanceUid },
        };

        AddRouteValues(routeValues, out bool hasPartition);

        var routeName = hasPartition
            ? KnownRouteNames.PartitionedRetrieveWorkitemInstance
            : KnownRouteNames.RetrieveWorkitemInstance;

        return RouteUri(routeName, routeValues);
    }

    /// <inheritdoc />
    public Uri ResolveRetrieveInstanceUri(InstanceIdentifier instanceIdentifier, bool isPartitionEnabled)
    {
        EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));

        var routeValues = new RouteValueDictionary
        {
            { KnownActionParameterNames.StudyInstanceUid, instanceIdentifier.StudyInstanceUid },
            { KnownActionParameterNames.SeriesInstanceUid, instanceIdentifier.SeriesInstanceUid },
            { KnownActionParameterNames.SopInstanceUid, instanceIdentifier.SopInstanceUid },
        };

        if (isPartitionEnabled)
        {
            routeValues.Add(KnownActionParameterNames.PartitionName, instanceIdentifier.Partition.Name);
        }

        var routeName = isPartitionEnabled
            ? KnownRouteNames.PartitionRetrieveInstance
            : KnownRouteNames.RetrieveInstance;

        return RouteUri(routeName, routeValues);
    }

    private void AddRouteValues(RouteValueDictionary routeValues, out bool hasPartition)
    {
        hasPartition = _httpContextAccessor.HttpContext.Request.RouteValues.TryGetValue(KnownActionParameterNames.PartitionName, out var partitionName);

        if (hasPartition)
        {
            routeValues.Add(KnownActionParameterNames.PartitionName, partitionName);
        }
    }

    private Uri RouteUri(string routeName, RouteValueDictionary routeValues)
    {
        HttpRequest request = _httpContextAccessor.HttpContext.Request;

        return GetRouteUri(
                ActionContext.HttpContext,
                routeName,
                routeValues,
                request.Scheme,
                request.Host.Value);
    }

    private Uri GetRouteUri(HttpContext httpContext, string routeName, RouteValueDictionary routeValues, string scheme, string host)
    {
        var uriString = string.Empty;

        if (httpContext == null)
        {
            uriString = UrlHelper.RouteUrl(routeName, routeValues, scheme, host);
        }
        else
        {
            var pathBase = httpContext.Request?.PathBase.ToString();
            uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, routeValues, scheme, new HostString(host), pathBase);
        }

        return new Uri(uriString);
    }
}
