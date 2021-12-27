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
using Microsoft.Health.Dicom.Core.Web;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [ApiVersion("1.0-prerelease")]
    [QueryModelStateValidator]
    [ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
    [ServiceFilter(typeof(PopulateDataPartitionFilterAttribute))]
    public class WorkitemsController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<WorkitemsController> _logger;

        public WorkitemsController(IMediator mediator, ILogger<WorkitemsController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// This action requests the creation of a UPS Instance on the Origin-Server. It corresponds to the UPS DIMSE N-CREATE operation.
        /// </summary>
        /// <remarks>
        /// The request body contains all the metadata to be stored in DICOM PS 3.18 JSON metadata.
        /// Any binary data contained in the message shall be inline.
        ///
        /// DICOM PS 3.19 XML metadata is not supported.
        /// </remarks>
        /// <returns></returns>
        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [Produces(KnownContentTypes.ApplicationDicomJson)]
        [Consumes(KnownContentTypes.ApplicationDicom, KnownContentTypes.MultipartRelated)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.UnsupportedMediaType)]
        [HttpPost]
        [VersionedPartitionRoute(KnownRoutes.CreateWorkitemInstancesRoute, Name = KnownRouteNames.VersionedPartitionWorkitemInstance)]
        [PartitionRoute(KnownRoutes.CreateWorkitemInstancesRoute, Name = KnownRouteNames.PartitionedWorkitemInstance)]
        [VersionedRoute(KnownRoutes.CreateWorkitemInstancesRoute, Name = KnownRouteNames.VersionedWorkitemInstance)]
        [Route(KnownRoutes.CreateWorkitemInstancesRoute, Name = KnownRouteNames.WorkitemInstance)]
        [AuditEventType(AuditEventSubType.Workitem)]
        public async Task<IActionResult> CreateUPSAsync()
        {
            return await PostAsync();
        }

        /// <summary>
        /// This action sets the attributes of a UPS Instance managed by the Origin-Server. It corresponds to the UPS DIMSE N-SET operation.
        /// </summary>
        /// <param name="upsInstanceUid">UID of the Unified Procedure Step Instance</param>
        /// <param name="transactionUid">
        /// The Transaction UID / Locking UID for the specified Unified Procedure Step Instance.
        /// If the UPS instance is currently in the SCHEDULED state, {transaction} shall not be specified.
        /// If the UPS instance is currently in the IN PROGRESS state, {transaction} shall be specified
        /// </param>
        /// <remarks>
        /// The request body describes changes to a single Unified Procedure Step Instance. It shall include all
        /// Attributes for which Attribute Values are to be set.The changes shall comply with all requirements
        /// described in PS 3.4 Section CC.2.6.2. DICOM PS 3.19 XML metadata is not supported.
        /// 
        /// Because the request will be treated as atomic (indivisible) and idempotent (repeat executions have no
        /// additional effect), all changes contained in the request shall leave the UPS instance in an internally consistent state.
        /// </remarks>
        /// <returns></returns>
        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [Produces(KnownContentTypes.ApplicationDicomJson)]
        [Consumes(KnownContentTypes.ApplicationDicom, KnownContentTypes.MultipartRelated)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.UnsupportedMediaType)]
        [HttpPost]
        [VersionedPartitionRoute(KnownRoutes.UpdateWorkitemInstanceRoute, Name = KnownRouteNames.VersionedPartitionWorkitemInstance)]
        [PartitionRoute(KnownRoutes.UpdateWorkitemInstanceRoute, Name = KnownRouteNames.PartitionedWorkitemInstance)]
        [VersionedRoute(KnownRoutes.UpdateWorkitemInstanceRoute, Name = KnownRouteNames.VersionedWorkitemInstance)]
        [Route(KnownRoutes.UpdateWorkitemInstanceRoute, Name = KnownRouteNames.WorkitemInstance)]
        [AuditEventType(AuditEventSubType.Workitem)]
        public async Task<IActionResult> UpdateUPSAsync(string upsInstanceUid, string transactionUid = null)
        {
            return await PostAsync(upsInstanceUid, transactionUid);
        }

        private async Task<IActionResult> PostAsync(string upsInstanceUid = null, string transactionUid = null)
        {
            long fileSize = Request.ContentLength ?? 0;
            _logger.LogInformation("DICOM Web Store Workitem Transaction request received, with UPS instance UID {UPSInstanceUid} and file size of {FileSize} bytes", upsInstanceUid, fileSize);

            var storeResponse = await _mediator.StoreDicomWorkitemAsync(
                Request.Body,
                Request.ContentType,
                upsInstanceUid, transactionUid,
                HttpContext.RequestAborted);

            return StatusCode(
                (int)storeResponse.Status.ToHttpStatusCode(),
                storeResponse.Workitem);
        }
    }
}
