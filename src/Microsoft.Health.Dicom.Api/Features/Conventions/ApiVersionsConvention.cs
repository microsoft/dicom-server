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
using System.Diagnostics;
using System.Globalization;

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
    private static readonly IReadOnlyList<ApiVersion> AllSupportedVersions = new List<ApiVersion>()
    {
        // this will result in null minor instead of 0 minor. There is no constructor on ApiVersion that allows this directly
        ApiVersion.Parse("1.0-prerelease"),
        ApiVersion.Parse("1"),
        ApiVersion.Parse("2"),
    };

    /// <summary>
    /// Add upcoming API versions here so they can be used for private previews.
    /// When upcomingVersion is ready for GA, move upcomingVersion to allSupportedVersion and remove from here.
    /// </summary>
    internal static IReadOnlyList<ApiVersion> UpcomingVersion = new List<ApiVersion>() { };

    internal const int CurrentVersion = 2;
    internal const int MinimumSupportedVersionForDicomUpdate = 2;

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

        var controllerIntroducedInVersion = controllerModel.Attributes
            .Where(a => a.GetType() == typeof(IntroducedInApiVersionAttribute))
            .Cast<IntroducedInApiVersionAttribute>()
            .Select(x => x.Version)
            .SingleOrDefault();

        IEnumerable<ApiVersion> versions = AllSupportedVersions;
        if (controllerIntroducedInVersion != null)
        {
            versions = GetAllSupportedVersions(controllerIntroducedInVersion.Value, CurrentVersion);
        }
        // when upcomingVersion is ready for GA, move upcomingVersion to allSupportedVersion
        versions = _isLatestApiVersionEnabled == true ? versions.Union(UpcomingVersion) : versions;
        controller.HasApiVersions(versions);

        var inactiveActions = controllerModel.Actions.Where(x => !IsEnabled(x, versions)).ToList();
        foreach (ActionModel action in inactiveActions)
        {
            controllerModel.Actions.Remove(action);
        }

        return true;
    }

    private static List<ApiVersion> GetAllSupportedVersions(int start, int end)
    {
        if (start < 1)
        {
            Debug.Fail("startApiVersion must be more >= 1");
        }
        if (end < start)
        {
            Debug.Fail("currentApiVersion must be >= startApiVersion");
        }

        return Enumerable
            .Range(start, end - start + 1)
            .Select(v => ApiVersion.Parse(v.ToString(CultureInfo.InvariantCulture)))
            .ToList();
    }

    private static bool IsEnabled(ActionModel actionModel, IEnumerable<ApiVersion> enabled)
    {
        HashSet<ApiVersion> actionVersions = actionModel.Attributes
            .Where(a => a is MapToApiVersionAttribute)
            .Cast<MapToApiVersionAttribute>()
            .SelectMany(x => x.Versions)
            .ToHashSet();

        if (actionVersions.Count == 0)
            return true;

        foreach (ApiVersion version in enabled)
        {
            if (actionVersions.Contains(version))
                return true;
        }

        return false;
    }
}
