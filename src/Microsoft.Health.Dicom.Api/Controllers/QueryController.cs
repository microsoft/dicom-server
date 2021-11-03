// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Api.Models;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Messages.Query;
using Microsoft.Health.Dicom.Core.Web;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [ApiVersion("1.0-prerelease")]
    [QueryModelStateValidator]
    [ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
    [ServiceFilter(typeof(PopulateDataPartitionFilterAttribute))]
    public class QueryController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<QueryController> _logger;

        public QueryController(IMediator mediator, ILogger<QueryController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [Produces(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [VersionedPartitionRoute(KnownRoutes.QueryAllStudiesRoute)]
        [PartitionRoute(KnownRoutes.QueryAllStudiesRoute)]
        [VersionedRoute(KnownRoutes.QueryAllStudiesRoute)]
        [Route(KnownRoutes.QueryAllStudiesRoute)]
        [AuditEventType(AuditEventSubType.Query)]
        public async Task<IActionResult> QueryForStudyAsync([FromQuery] QueryOptions options)
        {
            _logger.LogInformation("DICOM Web Query Study request received. QueryString {RequestQueryString}.", Request.QueryString);

            EnsureArg.IsNotNull(options);
            var response = await _mediator.QueryDicomResourcesAsync(
                options.ToParameters(Request.Query, QueryResource.AllStudies),
                cancellationToken: HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [HttpGet]
        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [Produces(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [VersionedPartitionRoute(KnownRoutes.QueryAllSeriesRoute)]
        [PartitionRoute(KnownRoutes.QueryAllSeriesRoute)]
        [VersionedRoute(KnownRoutes.QueryAllSeriesRoute)]
        [Route(KnownRoutes.QueryAllSeriesRoute)]
        [AuditEventType(AuditEventSubType.Query)]
        public async Task<IActionResult> QueryForSeriesAsync([FromQuery] QueryOptions options)
        {
            _logger.LogInformation("DICOM Web Query Series request received. QueryString {RequestQueryString}.", Request.QueryString);

            EnsureArg.IsNotNull(options);
            var response = await _mediator.QueryDicomResourcesAsync(
                options.ToParameters(Request.Query, QueryResource.AllSeries),
                cancellationToken: HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [HttpGet]
        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [Produces(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [VersionedPartitionRoute(KnownRoutes.QuerySeriesInStudyRoute)]
        [PartitionRoute(KnownRoutes.QuerySeriesInStudyRoute)]
        [VersionedRoute(KnownRoutes.QuerySeriesInStudyRoute)]
        [Route(KnownRoutes.QuerySeriesInStudyRoute)]
        [AuditEventType(AuditEventSubType.Query)]
        public async Task<IActionResult> QueryForSeriesInStudyAsync([FromRoute] string studyInstanceUid, [FromQuery] QueryOptions options)
        {
            _logger.LogInformation("DICOM Web Query Series request for study {studyInstanceUid} received. QueryString {RequestQueryString}.", studyInstanceUid, Request.QueryString);

            EnsureArg.IsNotNull(options);
            var response = await _mediator.QueryDicomResourcesAsync(
                options.ToParameters(Request.Query, QueryResource.StudySeries, studyInstanceUid: studyInstanceUid),
                cancellationToken: HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [HttpGet]
        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [Produces(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [VersionedPartitionRoute(KnownRoutes.QueryAllInstancesRoute)]
        [PartitionRoute(KnownRoutes.QueryAllInstancesRoute)]
        [VersionedRoute(KnownRoutes.QueryAllInstancesRoute)]
        [Route(KnownRoutes.QueryAllInstancesRoute)]
        [AuditEventType(AuditEventSubType.Query)]
        public async Task<IActionResult> QueryForInstancesAsync([FromQuery] QueryOptions options)
        {
            _logger.LogInformation("DICOM Web Query instances request received. QueryString {RequestQueryString}.", Request.QueryString);

            EnsureArg.IsNotNull(options);
            var response = await _mediator.QueryDicomResourcesAsync(
                options.ToParameters(Request.Query, QueryResource.AllInstances),
                cancellationToken: HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [HttpGet]
        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [Produces(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [VersionedPartitionRoute(KnownRoutes.QueryInstancesInStudyRoute)]
        [PartitionRoute(KnownRoutes.QueryInstancesInStudyRoute)]
        [VersionedRoute(KnownRoutes.QueryInstancesInStudyRoute)]
        [Route(KnownRoutes.QueryInstancesInStudyRoute)]
        [AuditEventType(AuditEventSubType.Query)]
        public async Task<IActionResult> QueryForInstancesInStudyAsync([FromRoute] string studyInstanceUid, [FromQuery] QueryOptions options)
        {
            _logger.LogInformation("DICOM Web Query Instances for study {studyInstanceUid} received. QueryString {RequestQueryString}.", studyInstanceUid, Request.QueryString);

            EnsureArg.IsNotNull(options);
            var response = await _mediator.QueryDicomResourcesAsync(
                options.ToParameters(Request.Query, QueryResource.StudyInstances, studyInstanceUid: studyInstanceUid),
                cancellationToken: HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [HttpGet]
        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [Produces(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [VersionedPartitionRoute(KnownRoutes.QueryInstancesInSeriesRoute)]
        [PartitionRoute(KnownRoutes.QueryInstancesInSeriesRoute)]
        [VersionedRoute(KnownRoutes.QueryInstancesInSeriesRoute)]
        [Route(KnownRoutes.QueryInstancesInSeriesRoute)]
        [AuditEventType(AuditEventSubType.Query)]
        public async Task<IActionResult> QueryForInstancesInSeriesAsync([FromRoute] string studyInstanceUid, [FromRoute] string seriesInstanceUid, [FromQuery] QueryOptions options)
        {
            _logger.LogInformation("DICOM Web Query Instances for study {studyInstanceUid} and series {seriesInstanceUid} received. QueryString {RequestQueryString}.", studyInstanceUid, seriesInstanceUid, Request.QueryString);

            EnsureArg.IsNotNull(options);
            var response = await _mediator.QueryDicomResourcesAsync(
                options.ToParameters(Request.Query, QueryResource.StudySeriesInstances, studyInstanceUid: studyInstanceUid, seriesInstanceUid: seriesInstanceUid),
                cancellationToken: HttpContext.RequestAborted);

            return CreateResult(response);
        }

        private IActionResult CreateResult(QueryResourceResponse resourceResponse)
        {
            Response.TryAddErroneousAttributesHeader(resourceResponse.ErroneousTags);
            if (!resourceResponse.ResponseDataset.Any())
            {
                return NoContent();
            }

            return StatusCode((int)HttpStatusCode.OK, resourceResponse.ResponseDataset);
        }
    }
}
