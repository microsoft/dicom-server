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
using Microsoft.Health.Dicom.Core.Messages.WorkitemMessages;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Net.Http.Headers;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [ApiVersion("1.0-prerelease")]
    [QueryModelStateValidator]
    [ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
    [ServiceFilter(typeof(UpsRsFeatureFilterAttribute))]
    [ServiceFilter(typeof(PopulateDataPartitionFilterAttribute))]
    public partial class WorkitemController : Controller
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
        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationJson }, allowSingle: true, allowMultiple: false)]
        [Produces(KnownContentTypes.ApplicationJson)]
        [Consumes(KnownContentTypes.ApplicationJson)]
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
        public async Task<IActionResult> AddAsync(string workitemInstanceUid = null)
        {
            return await PostAsync(workitemInstanceUid);
        }

        private async Task<IActionResult> PostAsync(string workitemInstanceUid)
        {
            long fileSize = Request.ContentLength ?? 0;
            _logger.LogInformation("DICOM Web Add Workitem Transaction request received, with Workitem instance UID {WorkitemInstanceUid}, and file size of {FileSize} bytes",
                workitemInstanceUid ?? string.Empty,
                fileSize);

            AddWorkitemResponse response = await _mediator.AddWorkitemAsync(
                Request.Body,
                Request.ContentType,
                workitemInstanceUid,
                HttpContext.RequestAborted);

            if (response.Status == WorkitemResponseStatus.Success)
            {
                Response.Headers.Add(HeaderNames.ContentLocation, response.Uri.ToString());
                Response.Headers.Add(HeaderNames.Location, response.Uri.ToString());
            }

            return StatusCode((int)response.Status.ToHttpStatusCode(), response.Message);
        }
    }
}
