// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using EnsureThat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Versioning.Conventions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using System.Diagnostics;

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
    private readonly ImmutableHashSet<ApiVersion> _allSupportedVersions = ImmutableHashSet.Create<ApiVersion>
    (
        new ApiVersion(1, 0, "prerelease"),
        new ApiVersion(1, 0)
    );

    private readonly ImmutableHashSet<ApiVersion> _upcomingVersion = ImmutableHashSet.Create<ApiVersion>
    (
        new ApiVersion(2, 0)
    );

    private readonly int _currentVersion = 1;
    private readonly bool _isLatestApiVersionEnabled;

    public ApiVersionsConvention(
        IOptions<FeatureConfiguration> featureConfiguration)
    {
        EnsureArg.IsNotNull(featureConfiguration, nameof(featureConfiguration));
        _isLatestApiVersionEnabled = featureConfiguration.Value.EnableLatestApiVersion;
    }

    public bool Apply(IControllerConventionBuilder controller, ControllerModel controllerModel)
    {
        EnsureArg.IsNotNull(controller, nameof(controller));
        EnsureArg.IsNotNull(controllerModel, nameof(controllerModel));

        var controllerIntroducedInVersion = controllerModel.Attributes
                .Where(a => a.GetType() == typeof(IntroducedInApiVersionAttribute))
                .Select(a => ((IntroducedInApiVersionAttribute)a).Version)
                .SingleOrDefault();

        var versions = _allSupportedVersions;
        if (controllerIntroducedInVersion != null)
        {
            versions = GetAllSupportedVersions(controllerIntroducedInVersion.Value, _currentVersion);
        }
        // when upcomingVersion is ready for GA, move upcomingVerion to allSupportedVersion and remove this logic
        versions = _isLatestApiVersionEnabled == true ? versions.Union(_upcomingVersion) : versions;

        controller.HasApiVersions(versions);
        return true;
    }

    internal static ImmutableHashSet<ApiVersion> GetAllSupportedVersions(int startApiVersion, int currentApiVersion)
    {
        if (startApiVersion < 1)
        {
            Debug.Fail("startApiVersion must be more >= 1");
        }
        if (currentApiVersion < startApiVersion)
        {
            Debug.Fail("currentApiVersion must be >= startApiVersion");
        }

        var currentVersion = new ApiVersion(currentApiVersion, 0);
        var allVersions = new List<ApiVersion>();

        for (int i = startApiVersion; i <= currentApiVersion; i++)
        {
            allVersions.Add(new ApiVersion(i, 0));
        }
        return allVersions.ToImmutableHashSet();
    }

}
