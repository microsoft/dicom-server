// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Api.Features.Exceptions;
using Microsoft.Health.Dicom.Api.Models;
using Microsoft.Health.Dicom.Core.Configs;

namespace Microsoft.Health.Dicom.Api.Features.Filters;

public sealed class ApiVersionValidatorAttribute : ActionFilterAttribute
{
    private readonly bool _isLatestApiVersionEnabled;

    public ApiVersionValidatorAttribute(
        IOptions<FeatureConfiguration> featureConfiguration)
    {
        EnsureArg.IsNotNull(featureConfiguration, nameof(featureConfiguration));
        _isLatestApiVersionEnabled = featureConfiguration.Value.EnableLatestApiVersion;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        int? apiMajorVersion = context.HttpContext.GetRequestedApiVersion()?.MajorVersion;
        if (apiMajorVersion != null && apiMajorVersion == Constants.ApiVersion.MaxVersion && !_isLatestApiVersionEnabled)
        {
            throw new ApiVersionNotSupportedException(Constants.ApiVersion.MaxVersion);
        }
    }
}
