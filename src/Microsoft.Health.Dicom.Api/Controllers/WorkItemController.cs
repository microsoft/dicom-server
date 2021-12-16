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
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Web;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [ApiVersion("1.0-prerelease")]
    [QueryModelStateValidator]
    [ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
    [ServiceFilter(typeof(PopulateDataPartitionFilterAttribute))]
    public class WorkitemController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<WorkitemController> _logger;

        public WorkitemController(IMediator mediator, ILogger<WorkitemController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [Produces(KnownContentTypes.ApplicationDicomJson)]
        [Consumes(KnownContentTypes.ApplicationDicom, KnownContentTypes.MultipartRelated)]
        [ProducesResponseType(typeof(Workitem), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Workitem), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType(typeof(Workitem), (int)HttpStatusCode.Conflict)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.UnsupportedMediaType)]
        [HttpPost]
        [VersionedPartitionRoute(KnownRoutes.WorkitemInstancesRoute, Name = KnownRouteNames.VersionedPartitionWorkitemInstance)]
        [PartitionRoute(KnownRoutes.WorkitemInstancesRoute, Name = KnownRouteNames.PartitionWorkitemInstance)]
        [VersionedRoute(KnownRoutes.WorkitemInstancesRoute, Name = KnownRouteNames.VersionedWorkitemInstance)]
        [Route(KnownRoutes.WorkitemInstancesRoute, Name = KnownRouteNames.WorkitemInstance)]
        [AuditEventType(AuditEventSubType.Workitem)]
        public async Task<IActionResult> PostInstanceAsync()
        {
            return await PostAsync(null);
        }

        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [Produces(KnownContentTypes.ApplicationDicomJson)]
        [Consumes(KnownContentTypes.ApplicationDicom, KnownContentTypes.MultipartRelated)]
        [ProducesResponseType(typeof(Workitem), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Workitem), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.UnsupportedMediaType)]
        [HttpPost]
        [VersionedPartitionRoute(KnownRoutes.WorkitemInstancesInWorkitemRoute, Name = KnownRouteNames.VersionedPartitionWorkitemInstancesInWorkitem)]
        [PartitionRoute(KnownRoutes.WorkitemInstancesInWorkitemRoute, Name = KnownRouteNames.PartitionWorkitemInstancesInWorkitem)]
        [VersionedRoute(KnownRoutes.WorkitemInstancesInWorkitemRoute, Name = KnownRouteNames.VersionedWorkitemInstancesInWorkitem)]
        [Route(KnownRoutes.WorkitemInstancesInWorkitemRoute, Name = KnownRouteNames.WorkitemInstancesInWorkitem)]
        [AuditEventType(AuditEventSubType.Workitem)]
        public async Task<IActionResult> PostInstanceInWorkitemAsync(string workItemInstanceUid)
        {
            return await PostAsync(workItemInstanceUid);
        }

        private async Task<IActionResult> PostAsync(string workItemInstanceUid)
        {
            long fileSize = Request.ContentLength ?? 0;
            _logger.LogInformation("DICOM Web Store Transaction request received, with work-item instance UID {WorkitemInstanceUid} and file size of {FileSize} bytes", workItemInstanceUid, fileSize);

            var storeResponse = await _mediator.StoreDicomWorkitemAsync(
                Request.Body,
                Request.ContentType,
                workItemInstanceUid,
                HttpContext.RequestAborted);

            return StatusCode(
                (int)storeResponse.Status.ToHttpStatusCode(),
                storeResponse.Workitem);
        }
    }
}
