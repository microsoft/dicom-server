// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Api.Features.Filters
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PopulateDataPartitionFilterAttribute : ActionFilterAttribute
    {
        private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
        private readonly IMediator _mediator;

        public PopulateDataPartitionFilterAttribute(
            IDicomRequestContextAccessor dicomRequestContextAccessor,
            IMediator mediator)
        {
            _dicomRequestContextAccessor = EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));
            _mediator = EnsureArg.IsNotNull(mediator, nameof(mediator));
        }

        public async override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            var svc = context.HttpContext.RequestServices;
            IOptions<FeatureConfiguration> featureConfiguration = svc.GetService<IOptions<FeatureConfiguration>>();
            var isPartitionEnabled = featureConfiguration.Value.EnableDataPartitions;
            IDicomRequestContext dicomRequestContext = _dicomRequestContextAccessor.RequestContext;
            RouteData routeData = context.RouteData;
            var routeName = context.ActionDescriptor?.AttributeRouteInfo?.Name;

            if (routeData?.Values != null)
            {
                // Try get Partition Name
                if (isPartitionEnabled && routeData.Values.TryGetValue(KnownActionParameterNames.PartitionName, out var partitionName))
                {
                    PartitionNameValidator.Validate(partitionName?.ToString());

                    var partitionResponse = await _mediator.GetPartitionAsync(partitionName.ToString());

                    if (partitionResponse?.PartitionEntry != null)
                    {
                        dicomRequestContext.DataPartitionEntry = partitionResponse.PartitionEntry;
                    }
                    else if (routeName == KnownRouteNames.PartitionStoreInstance ||
                        routeName == KnownRouteNames.VersionedPartitionStoreInstance ||
                        routeName == KnownRouteNames.PartitionStoreInstancesInStudy ||
                        routeName == KnownRouteNames.VersionedPartitionStoreInstancesInStudy
                        )
                    {
                        partitionResponse = await _mediator.AddPartitionAsync(partitionName.ToString());
                        dicomRequestContext.DataPartitionEntry = partitionResponse.PartitionEntry;
                    }
                    else
                    {
                        throw new DataPartitionsNotFoundPartitionException(partitionName.ToString());
                    }
                }
            }

            await base.OnActionExecutionAsync(context, next);
        }
    }
}
