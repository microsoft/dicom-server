// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Reflection;
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
        var controllerType = new TestController();
        var attributes = Array.Empty<object>();
        var controllerModel = new ControllerModel(controllerType.GetTypeInfo(), attributes);
        var controller = Substitute.For<IControllerConventionBuilder>();
        var featuresOptions = Options.Create(new FeatureConfiguration());
        var convention = new ApiVersionsConvention(featuresOptions);

        // act
        var applied = convention.Apply(controller, controllerModel);

        // assert
        ImmutableHashSet<ApiVersion> supportedVersions = ImmutableHashSet.Create<ApiVersion>
         (
             new ApiVersion(1, 0, "prerelease"),
             new ApiVersion(1, 0)
         );
        controller.Received().HasApiVersions(supportedVersions);
    }

    [Fact]
    public void GivenController_IntroducedInAttribute_CorrectVersionsApplied()
    {
        // arrange
        var controllerType = new TestController();
        var attributes = new object[] { new IntroducedInApiVersionAttribute(1) };
        var controllerModel = new ControllerModel(controllerType.GetTypeInfo(), attributes);
        var controller = Substitute.For<IControllerConventionBuilder>();
        var featuresOptions = Options.Create(new FeatureConfiguration());
        var convention = new ApiVersionsConvention(featuresOptions);

        // act
        var applied = convention.Apply(controller, controllerModel);

        // assert
        ImmutableHashSet<ApiVersion> supportedVersions = ImmutableHashSet.Create<ApiVersion>
         (
             new ApiVersion(1, 0)
         );
        controller.Received().HasApiVersions(supportedVersions);
    }

    [Fact]
    public void GivenController_IntroducedInAttribute_WhenNewApiTurnedOnCorrectVersionsApplied()
    {
        // arrange
        var controllerType = new TestController();
        var attributes = new object[] { new IntroducedInApiVersionAttribute(1) };
        var controllerModel = new ControllerModel(controllerType.GetTypeInfo(), attributes);
        var controller = Substitute.For<IControllerConventionBuilder>();
        var featuresOptions = Options.Create(new FeatureConfiguration { EnableLatestApiVersion = true });
        var convention = new ApiVersionsConvention(featuresOptions);

        // act
        var applied = convention.Apply(controller, controllerModel);

        // assert
        ImmutableHashSet<ApiVersion> supportedVersions = ImmutableHashSet.Create<ApiVersion>
         (
             new ApiVersion(1, 0),
             new ApiVersion(2, 0)
         );
        controller.Received().HasApiVersions(supportedVersions);
    }

    [Fact]
    public void GivenController_NoVersionAttribute_WhenNewApiTurnedOnCorrectVersionsApplied()
    {
        // arrange
        var controllerType = new TestController();
        var attributes = Array.Empty<object>();
        var controllerModel = new ControllerModel(controllerType.GetTypeInfo(), attributes);
        var controller = Substitute.For<IControllerConventionBuilder>();
        var featuresOptions = Options.Create(new FeatureConfiguration { EnableLatestApiVersion = true });
        var convention = new ApiVersionsConvention(featuresOptions);

        // act
        var applied = convention.Apply(controller, controllerModel);

        // assert
        ImmutableHashSet<ApiVersion> supportedVersions = ImmutableHashSet.Create<ApiVersion>
         (
             new ApiVersion(1, 0, "prerelease"),
             new ApiVersion(1, 0),
             new ApiVersion(2, 0)
         );
        controller.Received().HasApiVersions(supportedVersions);
    }
    private class TestController : TypeDelegator
    { }
}
