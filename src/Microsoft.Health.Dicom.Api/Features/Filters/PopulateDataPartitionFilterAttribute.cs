// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
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
        private readonly bool _isPartitionEnabled;

        private readonly HashSet<string> _storeRouteNames = new HashSet<string>
        {
            KnownRouteNames.PartitionStoreInstance,
            KnownRouteNames.VersionedPartitionStoreInstance,
            KnownRouteNames.PartitionStoreInstancesInStudy,
            KnownRouteNames.VersionedPartitionStoreInstancesInStudy
        };

        public PopulateDataPartitionFilterAttribute(
            IDicomRequestContextAccessor dicomRequestContextAccessor,
            IMediator mediator,
            IOptions<FeatureConfiguration> featureConfiguration)
        {
            _dicomRequestContextAccessor = EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));
            _mediator = EnsureArg.IsNotNull(mediator, nameof(mediator));

            EnsureArg.IsNotNull(featureConfiguration, nameof(featureConfiguration));
            _isPartitionEnabled = featureConfiguration.Value.EnableDataPartitions;
        }

        public async override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            IDicomRequestContext dicomRequestContext = _dicomRequestContextAccessor.RequestContext;
            RouteData routeData = context.RouteData;
            var routeName = context.ActionDescriptor?.AttributeRouteInfo?.Name;

            var routeContainsPartition = routeData.Values.TryGetValue(KnownActionParameterNames.PartitionName, out object value);

            if (!_isPartitionEnabled && routeContainsPartition) throw new DataPartitionsFeatureDisabledException();

            if (_isPartitionEnabled && !routeContainsPartition) throw new DataPartitionsMissingPartitionException();

            if (_isPartitionEnabled)
            {
                var partitionName = value?.ToString();

                PartitionNameValidator.Validate(partitionName);

                var partitionResponse = await _mediator.GetPartitionAsync(partitionName);

                if (partitionResponse?.PartitionEntry != null)
                {
                    dicomRequestContext.DataPartitionEntry = partitionResponse.PartitionEntry;
                }
                // Only for STOW, we create partition based on the request.
                // For all other requests, we validate whether it exists and process based on the result
                else if (_storeRouteNames.Contains(routeName))
                {
                    var response = await _mediator.AddPartitionAsync(partitionName);
                    dicomRequestContext.DataPartitionEntry = response.PartitionEntry;
                }
                else
                {
                    throw new DataPartitionsNotFoundException(partitionName);
                }
            }

            await base.OnActionExecutionAsync(context, next);
        }
    }
}
