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
    public class WorkItemController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<WorkItemController> _logger;

        public WorkItemController(IMediator mediator, ILogger<WorkItemController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [Produces(KnownContentTypes.ApplicationDicomJson)]
        [Consumes(KnownContentTypes.ApplicationDicom, KnownContentTypes.MultipartRelated)]
        [ProducesResponseType(typeof(WorkItem), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(WorkItem), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType(typeof(WorkItem), (int)HttpStatusCode.Conflict)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.UnsupportedMediaType)]
        [HttpPost]
        [VersionedPartitionRoute(KnownRoutes.WorkItemInstancesRoute, Name = KnownRouteNames.VersionedPartitionWorkItemInstance)]
        [PartitionRoute(KnownRoutes.WorkItemInstancesRoute, Name = KnownRouteNames.PartitionWorkItemInstance)]
        [VersionedRoute(KnownRoutes.WorkItemInstancesRoute, Name = KnownRouteNames.VersionedWorkItemInstance)]
        [Route(KnownRoutes.WorkItemInstancesRoute, Name = KnownRouteNames.WorkItemInstance)]
        [AuditEventType(AuditEventSubType.WorkItem)]
        public async Task<IActionResult> PostInstanceAsync()
        {
            return await PostAsync(null);
        }

        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [Produces(KnownContentTypes.ApplicationDicomJson)]
        [Consumes(KnownContentTypes.ApplicationDicom, KnownContentTypes.MultipartRelated)]
        [ProducesResponseType(typeof(WorkItem), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(WorkItem), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.UnsupportedMediaType)]
        [HttpPost]
        [VersionedPartitionRoute(KnownRoutes.WorkItemInstancesInWorkItemRoute, Name = KnownRouteNames.VersionedPartitionWorkItemInstancesInWorkItem)]
        [PartitionRoute(KnownRoutes.WorkItemInstancesInWorkItemRoute, Name = KnownRouteNames.PartitionWorkItemInstancesInWorkItem)]
        [VersionedRoute(KnownRoutes.WorkItemInstancesInWorkItemRoute, Name = KnownRouteNames.VersionedWorkItemInstancesInWorkItem)]
        [Route(KnownRoutes.WorkItemInstancesInWorkItemRoute, Name = KnownRouteNames.WorkItemInstancesInWorkItem)]
        [AuditEventType(AuditEventSubType.WorkItem)]
        public async Task<IActionResult> PostInstanceInWorkItemAsync(string workItemInstanceUid)
        {
            return await PostAsync(workItemInstanceUid);
        }

        private async Task<IActionResult> PostAsync(string workItemInstanceUid)
        {
            long fileSize = Request.ContentLength ?? 0;
            _logger.LogInformation("DICOM Web Store Transaction request received, with work-item instance UID {WorkItemInstanceUid} and file size of {FileSize} bytes", workItemInstanceUid, fileSize);

            var storeResponse = await _mediator.StoreDicomWorkItemAsync(
                Request.Body,
                Request.ContentType,
                workItemInstanceUid,
                HttpContext.RequestAborted);

            return StatusCode(
                (int)storeResponse.Status.ToHttpStatusCode(),
                storeResponse.WorkItem);
        }
    }
}
