// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Partition;

namespace Microsoft.Health.Dicom.Api.Features.Filters
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PopulateDataPartitionFilterAttribute : ActionFilterAttribute
    {
        private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
        private readonly IPartitionService _partitionService;

        public PopulateDataPartitionFilterAttribute(
            IDicomRequestContextAccessor dicomRequestContextAccessor,
            IPartitionService partitionService)
        {
            _dicomRequestContextAccessor = EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));
            _partitionService = EnsureArg.IsNotNull(partitionService, nameof(partitionService));
        }

        public async override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            var svc = context.HttpContext.RequestServices;
            IOptions<FeatureConfiguration> featureConfiguration = svc.GetService<IOptions<FeatureConfiguration>>();
            var isPartitionEnabled = featureConfiguration.Value.EnableDataPartitions;
            RouteData routeData = context.RouteData;
            IDicomRequestContext dicomRequestContext = _dicomRequestContextAccessor.RequestContext;

            if (routeData?.Values != null)
            {
                // Try get Partition Name
                if (isPartitionEnabled && routeData.Values.TryGetValue(KnownActionParameterNames.PartitionName, out object partitionName))
                {
                    var partitionEntry = await _partitionService.GetPartition(partitionName.ToString());
                    dicomRequestContext.DataPartitionEntry = partitionEntry;
                }
            }

            await base.OnActionExecutionAsync(context, next);
        }
    }
}
