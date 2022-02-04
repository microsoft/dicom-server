// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Api.Features.Filters
{
    public sealed class UpsRsFeatureFilterAttribute : ActionFilterAttribute
    {
        private readonly bool _isUpsRsEnabled;

        public UpsRsFeatureFilterAttribute(IOptions<FeatureConfiguration> featureConfiguration)
        {
            EnsureArg.IsNotNull(featureConfiguration, nameof(featureConfiguration));

            // UPS-RS can be enabled independently, but will be enabled if data partitions are enabled
            _isUpsRsEnabled = featureConfiguration.Value.EnableUpsRs || featureConfiguration.Value.EnableDataPartitions;
        }

        public async override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            if (!_isUpsRsEnabled)
            {
                throw new UpsRsFeatureDisabledException();
            }

            await base.OnActionExecutionAsync(context, next);
        }
    }
}
