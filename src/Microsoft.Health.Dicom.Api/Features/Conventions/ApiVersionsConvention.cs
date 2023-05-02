// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Versioning.Conventions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;

namespace Microsoft.Health.Dicom.Api.Features.Conventions;

/// <summary>
/// Handles the API version logic for our controllers
/// All GAed versions are mentioned in _allSupportedVersions
/// Preview version is mentioned in _upcomingVersion
/// Latest GAed version is mentioned in _currentVersion
/// use IntroducedInApiVersionAttribute to add new functionality on latest version only
/// </summary>
internal class ApiVersionsConvention : IControllerConvention
{
    private readonly bool _isLatestApiVersionEnabled;

    public ApiVersionsConvention(IOptions<FeatureConfiguration> featureConfiguration)
    {
        EnsureArg.IsNotNull(featureConfiguration, nameof(featureConfiguration));
        _isLatestApiVersionEnabled = featureConfiguration.Value.EnableLatestApiVersion;
    }

    public bool Apply(IControllerConventionBuilder controller, ControllerModel controllerModel)
    {
        EnsureArg.IsNotNull(controller, nameof(controller));
        EnsureArg.IsNotNull(controllerModel, nameof(controllerModel));

        (ApiVersion validStart, ApiVersion validEnd) = controllerModel.Attributes
            .Where(a => a.GetType() == typeof(ApiVersionRangeAttribute))
            .Cast<ApiVersionRangeAttribute>()
            .Select(x => (x.Start, x.End))
            .SingleOrDefault((DicomApiVersions.Earliest, DicomApiVersions.Latest));

        IEnumerable<ApiVersion> versions = DicomApiVersions
            .GetApiVersions(includeUnstable: _isLatestApiVersionEnabled)
            .Where(v => v >= validStart && v <= validEnd);

        controller.HasApiVersions(versions);
        return true;
    }
}
