// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
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
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Messages.Partition;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Filters
{
    public class PopulateDataPartitionFilterAttributeTests
    {
        private readonly ControllerActionDescriptor _controllerActionDescriptor;
        private readonly HttpContext _httpContext;
        private readonly ActionExecutingContext _actionExecutingContext;
        private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
        private readonly IMediator _mediator;
        private readonly IServiceProvider _serviceProvider;

        private const string ControllerName = "controller";
        private const string ActionName = "actionName";
        private const string RouteName = "routeName";

        private readonly PopulateDataPartitionFilterAttribute _filterAttribute;

        public PopulateDataPartitionFilterAttributeTests()
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

            var featureConfig = Options.Create(new FeatureConfiguration { EnableDataPartitions = true });

            _serviceProvider = Substitute.For<IServiceProvider>();
            _serviceProvider.GetService<IOptions<FeatureConfiguration>>()
                .Returns(featureConfig);

            _httpContext.RequestServices.Returns(_serviceProvider);

            _actionExecutingContext = new ActionExecutingContext(
                new ActionContext(_httpContext, new RouteData(), _controllerActionDescriptor),
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                FilterTestsHelper.CreateMockRetrieveController());

            var routeValueDictionary = new RouteValueDictionary
            {
                { KnownActionParameterNames.StudyInstanceUid, "123" },
                { KnownActionParameterNames.PartitionName, DefaultPartition.Name },
            };
            _actionExecutingContext.RouteData = new RouteData(routeValueDictionary);


            _dicomRequestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();

            _mediator = Substitute.For<IMediator>();
            _mediator.Send(Arg.Any<GetPartitionRequest>())
                .Returns(new GetPartitionResponse(new PartitionEntry(DefaultPartition.Key, DefaultPartition.Name)));

            _filterAttribute = new PopulateDataPartitionFilterAttribute(_dicomRequestContextAccessor, _mediator);
        }

        [Fact]
        public async Task GivenExistingPartitionNamePassed_ThenContextShouldBeSet()
        {
            await _filterAttribute.OnActionExecutionAsync(_actionExecutingContext, Substitute.For<ActionExecutionDelegate>());

            _dicomRequestContextAccessor.Received().RequestContext.DataPartitionEntry.PartitionKey = DefaultPartition.Key;
        }

        [Fact]
        public void GivenNonExistingPartitionNamePassed_ThenThrows()
        {
            _mediator.Send(Arg.Any<GetPartitionRequest>())
                .Returns(new GetPartitionResponse(null));

            Assert.ThrowsAsync<DataPartitionsNotFoundException>(async () => await _filterAttribute.OnActionExecutionAsync(_actionExecutingContext, Substitute.For<ActionExecutionDelegate>()));
        }

        [Fact]
        public async Task GivenNonExistingPartitionNamePassed_AndStowRequest_ThenPartitionIsCreated()
        {
            var newPartitionKey = 3;
            var newPartitionName = "partition";

            _controllerActionDescriptor.AttributeRouteInfo.Name = KnownRouteNames.PartitionStoreInstance;

            _mediator.Send(Arg.Any<GetPartitionRequest>())
                .Returns(new GetPartitionResponse(null));
            _mediator.Send(Arg.Any<AddPartitionRequest>())
                .Returns(new AddPartitionResponse(new PartitionEntry(newPartitionKey, newPartitionName)));

            await _filterAttribute.OnActionExecutionAsync(_actionExecutingContext, Substitute.For<ActionExecutionDelegate>());

            _dicomRequestContextAccessor.Received().RequestContext.DataPartitionEntry.PartitionKey = newPartitionKey;
        }
    }
}
