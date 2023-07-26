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

namespace Microsoft.Health.Dicom.Api.Features.Filters;

[AttributeUsage(AttributeTargets.Class)]
public sealed class PopulateDataPartitionFilterAttribute : ActionFilterAttribute
{
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
    private readonly IMediator _mediator;
    private readonly bool _isPartitionEnabled;

    private readonly HashSet<string> _partitionCreationSupportedRouteNames = new HashSet<string>
    {
        KnownRouteNames.PartitionStoreInstance,
        KnownRouteNames.PartitionStoreInstancesInStudy,
        KnownRouteNames.PartitionedAddWorkitemInstance,
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

        if (!_isPartitionEnabled && routeContainsPartition)
            throw new DataPartitionsFeatureDisabledException();

        if (_isPartitionEnabled && !routeContainsPartition)
            throw new DataPartitionsMissingPartitionException();

        if (_isPartitionEnabled)
        {
            var partitionName = value?.ToString();

            var partitionResponse = await _mediator.GetOrAddPartitionAsync(partitionName, _partitionCreationSupportedRouteNames.Contains(routeName));

            dicomRequestContext.DataPartition = partitionResponse.Partition;
        }

        await base.OnActionExecutionAsync(context, next);
    }
}
