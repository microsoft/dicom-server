// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Features.Context;

namespace Microsoft.Health.Dicom.Api.Features.Filters;

[AttributeUsage(AttributeTargets.Class)]
public sealed class DicomRequestContextRouteDataPopulatingFilterAttribute : ActionFilterAttribute
{
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;

    public DicomRequestContextRouteDataPopulatingFilterAttribute(
        IDicomRequestContextAccessor dicomRequestContextAccessor)
    {
        EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));

        _dicomRequestContextAccessor = dicomRequestContextAccessor;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        EnsureArg.IsNotNull(context, nameof(context));

        IDicomRequestContext dicomRequestContext = _dicomRequestContextAccessor.RequestContext;
        dicomRequestContext.RouteName = context.ActionDescriptor?.AttributeRouteInfo?.Name;

        dicomRequestContext.Version = context.HttpContext.GetRequestedApiVersion()?.MajorVersion;

        // Set StudyInstanceUid, SeriesInstanceUid, and SopInstanceUid based on the route data
        RouteData routeData = context.RouteData;

        if (routeData?.Values != null)
        {
            // Try to get StudyInstanceUid
            if (routeData.Values.TryGetValue(KnownActionParameterNames.StudyInstanceUid, out object studyInstanceUid))
            {
                dicomRequestContext.StudyInstanceUid = studyInstanceUid.ToString();

                // Try to get SeriesInstanceUid only if StudyInstanceUid was successfully fetched.
                if (routeData.Values.TryGetValue(KnownActionParameterNames.SeriesInstanceUid,
                        out object seriesInstanceUid))
                {
                    dicomRequestContext.SeriesInstanceUid = seriesInstanceUid.ToString();

                    // Try to get SopInstanceUid only if StudyInstanceUid and SeriesInstanceUid were fetched successfully.
                    if (routeData.Values.TryGetValue(KnownActionParameterNames.SopInstanceUid,
                            out object sopInstanceUid))
                    {
                        dicomRequestContext.SopInstanceUid = sopInstanceUid.ToString();
                    }
                }
            }
        }

        base.OnActionExecuting(context);
    }
}
