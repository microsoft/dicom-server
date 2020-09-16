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
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Messages.Query;
using Microsoft.Health.Dicom.Core.Web;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [ModelStateValidator]
    [ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
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
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [Route(KnownRoutes.QueryAllStudiesRoute)]
        [AuditEventType(AuditEventSubType.Query)]
        public async Task<IActionResult> QueryForStudyAsync()
        {
            _logger.LogInformation($"DICOM Web Query Study request received. QueryString '{Request.QueryString}.");

            var response = await _mediator.QueryDicomResourcesAsync(
                Request.Query,
                QueryResource.AllStudies,
                cancellationToken: HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [HttpGet]
        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [Route(KnownRoutes.QueryAllSeriesRoute)]
        [AuditEventType(AuditEventSubType.Query)]
        public async Task<IActionResult> QueryForSeriesAsync()
        {
            _logger.LogInformation($"DICOM Web Query Series request received. QueryString '{Request.QueryString}.");

            var response = await _mediator.QueryDicomResourcesAsync(
                Request.Query,
                QueryResource.AllSeries,
                cancellationToken: HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [HttpGet]
        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [Route(KnownRoutes.QuerySeriesInStudyRoute)]
        [AuditEventType(AuditEventSubType.Query)]
        public async Task<IActionResult> QueryForSeriesInStudyAsync(string studyInstanceUid)
        {
            _logger.LogInformation($"DICOM Web Query Series request for study '{studyInstanceUid}' received. QueryString '{Request.QueryString}.");

            var response = await _mediator.QueryDicomResourcesAsync(
                Request.Query,
                QueryResource.StudySeries,
                studyInstanceUid: studyInstanceUid,
                cancellationToken: HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [HttpGet]
        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [Route(KnownRoutes.QueryAllInstancesRoute)]
        [AuditEventType(AuditEventSubType.Query)]
        public async Task<IActionResult> QueryForInstancesAsync()
        {
            _logger.LogInformation($"DICOM Web Query instances request received. QueryString '{Request.QueryString}.");

            var response = await _mediator.QueryDicomResourcesAsync(
                Request.Query,
                QueryResource.AllInstances,
                cancellationToken: HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [HttpGet]
        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [Route(KnownRoutes.QueryInstancesInStudyRoute)]
        [AuditEventType(AuditEventSubType.Query)]
        public async Task<IActionResult> QueryForInstancesInStudyAsync(string studyInstanceUid)
        {
            _logger.LogInformation($"DICOM Web Query Instances for study '{studyInstanceUid}' received. QueryString '{Request.QueryString}.");

            var response = await _mediator.QueryDicomResourcesAsync(
                Request.Query,
                QueryResource.StudyInstances,
                studyInstanceUid: studyInstanceUid,
                cancellationToken: HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [HttpGet]
        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [Route(KnownRoutes.QueryInstancesInSeriesRoute)]
        [AuditEventType(AuditEventSubType.Query)]
        public async Task<IActionResult> QueryForInstancesInSeriesAsync(string studyInstanceUid, string seriesInstanceUid)
        {
            _logger.LogInformation($"DICOM Web Query Instances for study '{studyInstanceUid}' and series '{seriesInstanceUid}' received. QueryString '{Request.QueryString}.");

            var response = await _mediator.QueryDicomResourcesAsync(
                Request.Query,
                QueryResource.StudySeriesInstances,
                studyInstanceUid: studyInstanceUid,
                seriesInstanceUid: seriesInstanceUid,
                cancellationToken: HttpContext.RequestAborted);

            return CreateResult(response);
        }

        private IActionResult CreateResult(QueryResourceResponse resourceResponse)
        {
            if (!resourceResponse.ResponseDataset.Any())
            {
                return NoContent();
            }

            return StatusCode((int)HttpStatusCode.OK, resourceResponse.ResponseDataset);
        }
    }
}
