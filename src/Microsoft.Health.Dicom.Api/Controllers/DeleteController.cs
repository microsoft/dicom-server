// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Messages.Delete;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [ApiVersion("1.0-prerelease")]
    [QueryModelStateValidator]
    [ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
    public class DeleteController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<DeleteController> _logger;

        public DeleteController(IMediator mediator, ILogger<DeleteController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [VersionedRoute(KnownRoutes.StudyRoute)]
        [Route(KnownRoutes.StudyRoute)]
        [AuditEventType(AuditEventSubType.Delete)]
        public async Task<IActionResult> DeleteStudyAsync(string studyInstanceUid)
        {
            _logger.LogInformation("DICOM Web Delete Study request received, with study instance UID {studyInstanceUid}.", studyInstanceUid);

            DeleteResourcesResponse deleteResponse = await _mediator.DeleteDicomStudyAsync(
                studyInstanceUid, cancellationToken: HttpContext.RequestAborted);

            return NoContent();
        }

        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [VersionedRoute(KnownRoutes.SeriesRoute)]
        [Route(KnownRoutes.SeriesRoute)]
        [AuditEventType(AuditEventSubType.Delete)]
        public async Task<IActionResult> DeleteSeriesAsync(string studyInstanceUid, string seriesInstanceUid)
        {
            _logger.LogInformation("DICOM Web Delete Series request received, with study instance UID {studyInstanceUid} and series UID {seriesInstanceUid}.", studyInstanceUid, seriesInstanceUid);

            DeleteResourcesResponse deleteResponse = await _mediator.DeleteDicomSeriesAsync(
                studyInstanceUid, seriesInstanceUid, cancellationToken: HttpContext.RequestAborted);

            return NoContent();
        }

        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [VersionedRoute(KnownRoutes.InstanceRoute)]
        [Route(KnownRoutes.InstanceRoute)]
        [AuditEventType(AuditEventSubType.Delete)]
        public async Task<IActionResult> DeleteInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            _logger.LogInformation("DICOM Web Delete Instance request received, with study instance UID {studyInstanceUid}, series UID {seriesInstanceUid} and instance UID {sopInstanceUid}.", studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            DeleteResourcesResponse deleteResponse = await _mediator.DeleteDicomInstanceAsync(
                studyInstanceUid, seriesInstanceUid, sopInstanceUid, cancellationToken: HttpContext.RequestAborted);

            return NoContent();
        }
    }
}
