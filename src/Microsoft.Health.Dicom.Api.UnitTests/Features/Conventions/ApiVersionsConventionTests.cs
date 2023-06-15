// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Versioning.Conventions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Api.Features.Conventions;
using Microsoft.Health.Dicom.Core.Configs;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Conventions;

public class ApiVersionsConventionTests
{
    [Fact]
    public void GivenController_NoVersionAttribute_AllSupportedVersionsApplied()
    {
        // arrange
        var controllerModel = new ControllerModel(typeof(TestController).GetTypeInfo(), Array.Empty<object>());
        var controller = Substitute.For<IControllerConventionBuilder>();
        var featuresOptions = Options.Create(new FeatureConfiguration());
        var convention = new ApiVersionsConvention(featuresOptions);

        // act
        convention.Apply(controller, controllerModel);

        // assert
        var supportedVersions = ImmutableHashSet.Create(new ApiVersion(1, 0, "prerelease"), new ApiVersion(1, 0));
        controller.Received().HasApiVersions(supportedVersions);
    }

    [Fact]
    public void GivenController_IntroducedInAttribute_CorrectVersionsApplied()
    {
        // arrange
        var attributes = new object[] { new IntroducedInApiVersionAttribute(1) };
        var controllerModel = new ControllerModel(typeof(TestController).GetTypeInfo(), attributes);
        var controller = Substitute.For<IControllerConventionBuilder>();
        var featuresOptions = Options.Create(new FeatureConfiguration());
        var convention = new ApiVersionsConvention(featuresOptions);

        // act
        convention.Apply(controller, controllerModel);

        // assert
        var supportedVersions = ImmutableHashSet.Create(new ApiVersion(1, 0));
        controller.Received().HasApiVersions(supportedVersions);
    }

    [Fact]
    public void GivenController_IntroducedInAttribute_WhenNewApiTurnedOnCorrectVersionsApplied()
    {
        // arrange
        var attributes = new object[] { new IntroducedInApiVersionAttribute(1) };
        var controllerModel = new ControllerModel(typeof(TestController).GetTypeInfo(), attributes);
        var controller = Substitute.For<IControllerConventionBuilder>();
        var featuresOptions = Options.Create(new FeatureConfiguration { EnableLatestApiVersion = true });
        var convention = new ApiVersionsConvention(featuresOptions);

        // act
        convention.Apply(controller, controllerModel);

        // assert
        var supportedVersions = ImmutableHashSet.Create(new ApiVersion(1, 0), new ApiVersion(2, 0));
        controller.Received().HasApiVersions(supportedVersions);
    }

    [Fact]
    public void GivenController_NoVersionAttribute_WhenNewApiTurnedOnCorrectVersionsApplied()
    {
        // arrange
        var controllerModel = new ControllerModel(typeof(TestController).GetTypeInfo(), Array.Empty<object>());
        var controller = Substitute.For<IControllerConventionBuilder>();
        var featuresOptions = Options.Create(new FeatureConfiguration { EnableLatestApiVersion = true });
        var convention = new ApiVersionsConvention(featuresOptions);

        // act
        convention.Apply(controller, controllerModel);

        // assert
        var supportedVersions = ImmutableHashSet.Create(new ApiVersion(1, 0, "prerelease"), new ApiVersion(1, 0), new ApiVersion(2, 0));
        controller.Received().HasApiVersions(supportedVersions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GivenActionInLatest_WhenLatestConfigured_ThenAddOrRemove(bool enableLatest)
    {
        // arrange
        MethodInfo actionMethod = typeof(TestController).GetMethod(nameof(TestController.GetResultAsync));
        var controllerModel = new ControllerModel(typeof(TestController).GetTypeInfo(), Array.Empty<object>())
        {
            Actions = { new ActionModel(actionMethod, actionMethod.GetCustomAttributes().ToList()) },
        };
        var builder = new ControllerApiVersionConventionBuilder(typeof(TestController));
        var featuresOptions = Options.Create(new FeatureConfiguration { EnableLatestApiVersion = enableLatest });
        var convention = new ApiVersionsConvention(featuresOptions);
        int nextVersion = ApiVersionsConvention.CurrentVersion + 1;
        ApiVersionsConvention.UpcomingVersion = new List<ApiVersion>() { ApiVersion.Parse(nextVersion.ToString(CultureInfo.InvariantCulture)) };

        // act
        convention.Apply(builder, controllerModel);

        // assert
        if (enableLatest)
            Assert.Equal(1, controllerModel.Actions.Count);
        else
            Assert.Empty(controllerModel.Actions);
    }

    private sealed class TestController : ControllerBase
    {
        [MapToApiVersion("3.0")]
        public Task<IActionResult> GetResultAsync()
            => throw new NotImplementedException();
    }
}
