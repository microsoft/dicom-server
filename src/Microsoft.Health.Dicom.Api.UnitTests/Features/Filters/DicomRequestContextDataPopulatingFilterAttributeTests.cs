// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Api.UnitTests.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Messages;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Filters
{
    public class DicomRequestContextDataPopulatingFilterAttributeTests
    {
        private readonly ControllerActionDescriptor _controllerActionDescriptor;
        private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();
        private readonly DefaultDicomRequestContext _dicomRequestContext = new DefaultDicomRequestContext();
        private readonly IAuditEventTypeMapping _auditEventTypeMapping = Substitute.For<IAuditEventTypeMapping>();
        private readonly HttpContext _httpContext = new DefaultHttpContext();
        private readonly ActionExecutingContext _actionExecutingContext;
        private const string ControllerName = "controller";
        private const string ActionName = "actionName";
        private const string RouteName = "routeName";
        private const string NormalAuditEventType = "event-name";
        private const string PartitionId = "partition1";
        private const string StudyInstanceUid = "123";
        private const string SeriesInstanceUid = "456";
        private const string SopInstanceUid = "789";

        private readonly DicomRequestContextRouteDataPopulatingFilterAttribute _filterAttribute;

        public DicomRequestContextDataPopulatingFilterAttributeTests()
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

            _actionExecutingContext = new ActionExecutingContext(
                new ActionContext(_httpContext, new RouteData(), _controllerActionDescriptor),
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                FilterTestsHelper.CreateMockRetrieveController());

            _dicomRequestContextAccessor.RequestContext.Returns(_dicomRequestContext);

            _filterAttribute = new DicomRequestContextRouteDataPopulatingFilterAttribute(_dicomRequestContextAccessor, _auditEventTypeMapping);
        }

        [Fact]
        public void GivenRetrieveRequestForStudy_WhenExecutingAnAction_ThenValuesShouldBeSetOnDicomFhirRequestContext()
        {
            var routeValueDictionary = new RouteValueDictionary
            {
                { KnownActionParameterNames.StudyInstanceUid, "123" },
            };
            _actionExecutingContext.RouteData = new RouteData(routeValueDictionary);

            ExecuteAndValidateFilter(NormalAuditEventType, NormalAuditEventType, ResourceType.Study);
        }

        [Fact]
        public void GivenRetrieveRequestForSeries_WhenExecutingAnAction_ThenValuesShouldBeSetOnDicomFhirRequestContext()
        {
            var routeValueDictionary = new RouteValueDictionary
            {
                { KnownActionParameterNames.StudyInstanceUid, StudyInstanceUid },
                { KnownActionParameterNames.SeriesInstanceUid, SeriesInstanceUid },
            };
            _actionExecutingContext.RouteData = new RouteData(routeValueDictionary);

            ExecuteAndValidateFilter(NormalAuditEventType, NormalAuditEventType, ResourceType.Series);
        }

        [Fact]
        public void GivenRetrieveRequestForSopInstance_WhenExecutingAnAction_ThenValuesShouldBeSetOnDicomFhirRequestContext()
        {
            var routeValueDictionary = new RouteValueDictionary
            {
                { KnownActionParameterNames.StudyInstanceUid, StudyInstanceUid },
                { KnownActionParameterNames.SeriesInstanceUid, SeriesInstanceUid },
                { KnownActionParameterNames.SopInstanceUid, SopInstanceUid },
            };
            _actionExecutingContext.RouteData = new RouteData(routeValueDictionary);

            ExecuteAndValidateFilter(NormalAuditEventType, NormalAuditEventType, ResourceType.Series);
        }

        [Fact]
        public void GivenRetrieveRequestForSopInstance_WhenPartitionIsSpecified_ThenPartitionIdShouldBeSetOnDicomRequestContext()
        {
            var routeValueDictionary = new RouteValueDictionary
            {
                { KnownActionParameterNames.PartitionId, PartitionId },
                { KnownActionParameterNames.StudyInstanceUid, StudyInstanceUid },
                { KnownActionParameterNames.SeriesInstanceUid, SeriesInstanceUid },
                { KnownActionParameterNames.SopInstanceUid, SopInstanceUid },
            };
            _actionExecutingContext.RouteData = new RouteData(routeValueDictionary);

            ExecuteAndValidateFilter(NormalAuditEventType, NormalAuditEventType, ResourceType.Series, true);
        }

        private void ExecuteAndValidateFilter(
            string auditEventTypeFromMapping,
            string expectedAuditEventType,
            ResourceType? resourceType = null,
            bool partitionsEnabled = false)
        {
            _auditEventTypeMapping.GetAuditEventType(ControllerName, ActionName).Returns(auditEventTypeFromMapping);

            _filterAttribute.OnActionExecuting(_actionExecutingContext);

            Assert.NotNull(_dicomRequestContextAccessor.RequestContext.AuditEventType);
            Assert.Equal(expectedAuditEventType, _dicomRequestContextAccessor.RequestContext.AuditEventType);
            Assert.Equal(RouteName, _dicomRequestContextAccessor.RequestContext.RouteName);

            if (resourceType != null)
            {
                if (partitionsEnabled)
                {
                    Assert.Equal(_dicomRequestContextAccessor.RequestContext.PartitionId, PartitionId);
                }
                else
                {
                    Assert.Null(_dicomRequestContextAccessor.RequestContext.PartitionId);
                }

                switch (resourceType)
                {
                    case ResourceType.Study:
                        Assert.Equal(_dicomRequestContextAccessor.RequestContext.StudyInstanceUid, StudyInstanceUid);
                        break;
                    case ResourceType.Series:
                        Assert.Equal(_dicomRequestContextAccessor.RequestContext.StudyInstanceUid, StudyInstanceUid);
                        Assert.Equal(_dicomRequestContextAccessor.RequestContext.SeriesInstanceUid, SeriesInstanceUid);
                        break;
                    case ResourceType.Instance:
                    case ResourceType.Frames:
                        Assert.Equal(_dicomRequestContextAccessor.RequestContext.StudyInstanceUid, StudyInstanceUid);
                        Assert.Equal(_dicomRequestContextAccessor.RequestContext.SeriesInstanceUid, SeriesInstanceUid);
                        Assert.Equal(_dicomRequestContextAccessor.RequestContext.SopInstanceUid, SopInstanceUid);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
