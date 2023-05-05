// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Context;

namespace Microsoft.Health.Dicom.Api.Features.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class PopulateOriginalRequestFilterAttribute : ActionFilterAttribute
{
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
    private readonly bool _isUpdateEnabled;

    public PopulateOriginalRequestFilterAttribute(
        IDicomRequestContextAccessor dicomRequestContextAccessor,
        IOptions<FeatureConfiguration> featureConfiguration)
    {
        _dicomRequestContextAccessor = EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));

        EnsureArg.IsNotNull(featureConfiguration, nameof(featureConfiguration));
        _isUpdateEnabled = featureConfiguration.Value.EnableUpdate;
    }

    public async override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        IDicomRequestContext dicomRequestContext = _dicomRequestContextAccessor.RequestContext;

        bool isOriginalVersionRequested = context.HttpContext.Request.IsOriginalVersionRequested();

        if (!_isUpdateEnabled && isOriginalVersionRequested)
            throw new DicomUpdateFeatureDisabledException();

        dicomRequestContext.IsOriginalRequested = isOriginalVersionRequested;

        await base.OnActionExecutionAsync(context, next);
    }
}
