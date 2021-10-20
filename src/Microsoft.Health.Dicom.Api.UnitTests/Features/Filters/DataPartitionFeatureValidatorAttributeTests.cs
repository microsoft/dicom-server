// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Filters
{
    public class DataPartitionFeatureValidatorAttributeTests
    {
        private readonly ControllerActionDescriptor _controllerActionDescriptor;
        private readonly HttpContext _httpContext;
        private readonly ActionExecutingContext _actionExecutingContext;
        private IServiceProvider _serviceProvider;
        private const string ControllerName = "controller";
        private const string ActionName = "actionName";
        private const string RouteName = "routeName";

        private readonly DataPartitionFeatureValidatorAttribute _filterAttribute;

        public DataPartitionFeatureValidatorAttributeTests()
        {
            _controllerActionDescriptor = new ControllerActionDescriptor
            {
                DisplayName = "Executing Context Test Descriptor",
                ActionName = ActionName,
                ControllerName = ControllerName,
                AttributeRouteInfo = new AttributeRouteInfo
                {
                    Name = RouteName,
                },
            };

            _httpContext = Substitute.For<HttpContext>();

            _actionExecutingContext = new ActionExecutingContext(
                new ActionContext(_httpContext, new RouteData(), _controllerActionDescriptor),
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                FilterTestsHelper.CreateMockRetrieveController());

            _filterAttribute = new DataPartitionFeatureValidatorAttribute();
        }

        [Fact]
        public void GivenRetrieveRequestWithDataPartitionsEnabled_WhenNoPartitionId_ThenItShouldThrowError()
        {
            var routeValueDictionary = new RouteValueDictionary
            {
                { KnownActionParameterNames.StudyInstanceUid, "123" },
            };
            _actionExecutingContext.RouteData = new RouteData(routeValueDictionary);

            SetupDataPartitionsFeatureFlag(true);

            Assert.Throws<DataPartitionsMissingPartitionException>(() => _filterAttribute.OnActionExecuting(_actionExecutingContext));
        }

        [Fact]
        public void GivenRetrieveRequestWithDataPartitionsDisabled_WhenPartitionIdIsPassed_ThenItShouldThrowError()
        {
            var routeValueDictionary = new RouteValueDictionary
            {
                { KnownActionParameterNames.StudyInstanceUid, "123" },
                { KnownActionParameterNames.PartitionName, "partition1" },
            };
            _actionExecutingContext.RouteData = new RouteData(routeValueDictionary);

            SetupDataPartitionsFeatureFlag(false);

            Assert.Throws<DataPartitionsFeatureDisabledException>(() => _filterAttribute.OnActionExecuting(_actionExecutingContext));
        }

        [Fact]
        public void GivenRetrieveRequestWithDataPartitionsEnabled_WhenPartitionIdIsPassed_ThenItExecutesSuccessfully()
        {
            var routeValueDictionary = new RouteValueDictionary
            {
                { KnownActionParameterNames.StudyInstanceUid, "123" },
                { KnownActionParameterNames.PartitionName, "partition1" },
            };
            _actionExecutingContext.RouteData = new RouteData(routeValueDictionary);

            SetupDataPartitionsFeatureFlag(true);

            _filterAttribute.OnActionExecuting(_actionExecutingContext);
        }

        [Fact]
        public void GivenRetrieveRequestWithDataPartitionsDisabled_WhenNoPartitionId_ThenItExecutesSuccessfully()
        {
            var routeValueDictionary = new RouteValueDictionary
            {
                { KnownActionParameterNames.StudyInstanceUid, "123" },
            };
            _actionExecutingContext.RouteData = new RouteData(routeValueDictionary);

            SetupDataPartitionsFeatureFlag(false);

            _filterAttribute.OnActionExecuting(_actionExecutingContext);
        }

        private void SetupDataPartitionsFeatureFlag(bool enableDataPartitions)
        {
            var featureConfig = Options.Create(new FeatureConfiguration { EnableDataPartitions = enableDataPartitions });

            _serviceProvider = Substitute.For<IServiceProvider>();
            _serviceProvider.GetService<IOptions<FeatureConfiguration>>()
                .Returns(featureConfig);

            _httpContext.RequestServices.Returns(_serviceProvider);
        }
    }
}
