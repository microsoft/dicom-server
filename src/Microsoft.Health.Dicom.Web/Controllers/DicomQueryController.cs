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
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Messages.Query;

namespace Microsoft.Health.Dicom.Web.Controllers
{
    public class DicomQueryController : Controller
    {
        private const string WarningHeaderName = "warning";
        private const string ApplicationDicomJson = "application/dicom+json";
        private readonly IMediator _mediator;
        private readonly ILogger<DicomQueryController> _logger;

        public DicomQueryController(IMediator mediator, ILogger<DicomQueryController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        [AcceptContentFilter(ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [HttpGet]
        [Route("studies/")]
        public async Task<IActionResult> QueryStudiesAsync(string[] includeField = null, bool fuzzyMatching = false, int? limit = null, int offset = 0)
        {
            _logger.LogInformation($"DICOM Web Query Studies Transaction request received.");

            QueryDicomResourcesResponse response = await _mediator.QueryDicomStudiesAsync(includeField, fuzzyMatching, limit, offset);
            return ConvertToActionResult(response);
        }

        [AcceptContentFilter(ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/series/")]
        [Route("series/")]
        public async Task<IActionResult> QuerySeriesAsync(string[] includeField = null, bool fuzzyMatching = false, int? limit = null, int offset = 0, string studyInstanceUID = null)
        {
            _logger.LogInformation($"DICOM Web Query Series Transaction request received.");

            QueryDicomResourcesResponse response = await _mediator.QueryDicomSeriesAsync(includeField, fuzzyMatching, limit, offset, studyInstanceUID);
            return ConvertToActionResult(response);
        }

        [AcceptContentFilter(ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/instances")]
        [Route("studies/{studyInstanceUID}/series/{seriesInstanceUID}/instances")]
        [Route("instances/")]
        public async Task<IActionResult> QueryInstancesAsync(string[] includeField = null, bool fuzzyMatching = false, int? limit = null, int offset = 0)
        {
            _logger.LogInformation($"DICOM Web Query Instances Transaction request received.");

            QueryDicomResourcesResponse response = await _mediator.QueryDicomStudiesAsync(includeField, fuzzyMatching, limit, offset);
            return ConvertToActionResult(response);
        }

        private IActionResult ConvertToActionResult(QueryDicomResourcesResponse response)
        {
            if (response.HasWarning)
            {
                Response.Headers.Add(WarningHeaderName, new StringValues(response.Warnings.ToArray()));
            }

            if (response.StatusCode == (int)HttpStatusCode.OK)
            {
                return StatusCode(response.StatusCode, response.ResponseMetadata);
            }

            return StatusCode(response.StatusCode);
        }
    }
}
