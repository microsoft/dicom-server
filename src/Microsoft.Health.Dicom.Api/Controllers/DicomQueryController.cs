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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Query;

namespace Microsoft.Health.Dicom.Api
{
    [Authorize]
    public class DicomQueryController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<DicomQueryController> _logger;

        public DicomQueryController(IMediator mediator, ILogger<DicomQueryController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [AcceptContentFilter(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [Route("studies")]
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
        [AcceptContentFilter(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [Route("series")]
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
        [AcceptContentFilter(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [Route("studies/{studyInstanceUid}/series")]
        public async Task<IActionResult> QueryForSeriesInStudyAsync(string studyInstanceUid)
        {
            EnsureArg.IsNotEmptyOrWhitespace(studyInstanceUid, nameof(studyInstanceUid));

            _logger.LogInformation($"DICOM Web Query Series request for study '{studyInstanceUid}' received. QueryString '{Request.QueryString}.");

            var response = await _mediator.QueryDicomResourcesAsync(
                Request.Query,
                QueryResource.StudySeries,
                studyInstanceUid: studyInstanceUid,
                cancellationToken: HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [HttpGet]
        [AcceptContentFilter(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [Route("instances")]
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
        [AcceptContentFilter(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [Route("studies/{studyInstanceUid}/instances")]
        public async Task<IActionResult> QueryForInstancesInStudyAsync(string studyInstanceUid)
        {
            EnsureArg.IsNotEmptyOrWhitespace(studyInstanceUid, nameof(studyInstanceUid));

            _logger.LogInformation($"DICOM Web Query Instances for study '{studyInstanceUid}' received. QueryString '{Request.QueryString}.");

            var response = await _mediator.QueryDicomResourcesAsync(
                Request.Query,
                QueryResource.StudyInstances,
                studyInstanceUid: studyInstanceUid,
                cancellationToken: HttpContext.RequestAborted);

            return CreateResult(response);
        }

        [HttpGet]
        [AcceptContentFilter(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [Route("studies/{studyInstanceUid}/series/{seriesInstanceUid}/instances")]
        public async Task<IActionResult> QueryForInstancesInSeriesAsync(string studyInstanceUid, string seriesInstanceUid)
        {
            EnsureArg.IsNotEmptyOrWhitespace(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotEmptyOrWhitespace(seriesInstanceUid, nameof(seriesInstanceUid));

            _logger.LogInformation($"DICOM Web Query Instances for study '{studyInstanceUid}' and series '{seriesInstanceUid}' received. QueryString '{Request.QueryString}.");

            var response = await _mediator.QueryDicomResourcesAsync(
                Request.Query,
                QueryResource.StudySeriesInstances,
                studyInstanceUid: studyInstanceUid,
                seriesInstanceUid: seriesInstanceUid,
                cancellationToken: HttpContext.RequestAborted);

            return CreateResult(response);
        }

        private IActionResult CreateResult(QueryDicomResourceResponse resourceResponse)
        {
            if (!resourceResponse.ResponseDataset.Any())
            {
                return NoContent();
            }

            return StatusCode((int)HttpStatusCode.OK, resourceResponse.ResponseDataset);
        }
    }
}
