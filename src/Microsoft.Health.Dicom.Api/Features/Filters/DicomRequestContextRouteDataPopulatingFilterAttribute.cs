// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Features.Context;

namespace Microsoft.Health.Dicom.Api.Features.Filters
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DicomRequestContextRouteDataPopulatingFilterAttribute : ActionFilterAttribute
    {
        private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
        private readonly IAuditEventTypeMapping _auditEventTypeMapping;

        public DicomRequestContextRouteDataPopulatingFilterAttribute(
            IDicomRequestContextAccessor dicomRequestContextAccessor,
            IAuditEventTypeMapping auditEventTypeMapping)
        {
            EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));
            EnsureArg.IsNotNull(auditEventTypeMapping, nameof(auditEventTypeMapping));

            _dicomRequestContextAccessor = dicomRequestContextAccessor;
            _auditEventTypeMapping = auditEventTypeMapping;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            IDicomRequestContext dicomRequestContext = _dicomRequestContextAccessor.DicomRequestContext;
            dicomRequestContext.RouteName = context.ActionDescriptor?.AttributeRouteInfo?.Name;

            // Set StudyInstanceUid, SeriesInstanceUid, and SopInstanceUid based on the route data
            RouteData routeData = context.RouteData;

            if (routeData?.Values != null)
            {
                // Try to get StudyInstanceUid
                if (routeData.Values.TryGetValue(KnownActionParameterNames.StudyInstanceUid, out object studyInstanceUid))
                {
                    dicomRequestContext.StudyInstanceUid = studyInstanceUid.ToString();

                    // Try to get SeriesInstanceUid only if StudyInstanceUid was successfully fetched.
                    if (routeData.Values.TryGetValue(KnownActionParameterNames.SeriesInstanceUid, out object seriesInstanceUid))
                    {
                        dicomRequestContext.SeriesInstanceUid = seriesInstanceUid.ToString();

                        // Try to get SopInstanceUid only if StudyInstanceUid and SeriesInstanceUid were fetched successfully.
                        if (routeData.Values.TryGetValue(KnownActionParameterNames.SopInstanceUid, out object sopInstanceUid))
                        {
                            dicomRequestContext.SopInstanceUid = sopInstanceUid.ToString();
                        }
                    }
                }
            }

            if (context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
            {
                dicomRequestContext.AuditEventType = _auditEventTypeMapping.GetAuditEventType(
                    controllerActionDescriptor.ControllerName,
                    controllerActionDescriptor.ActionName);
            }

            base.OnActionExecuting(context);
        }
    }
}
