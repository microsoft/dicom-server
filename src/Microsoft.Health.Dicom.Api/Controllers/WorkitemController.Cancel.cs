// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    public partial class WorkitemController
    {
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
        [VersionedPartitionRoute(KnownRoutes.CancelWorkitemInstancesRoute, Name = KnownRouteNames.VersionedPartitionCancelWorkitemInstance)]
        [PartitionRoute(KnownRoutes.CancelWorkitemInstancesRoute, Name = KnownRouteNames.PartitionedCancelWorkitemInstance)]
        [VersionedRoute(KnownRoutes.CancelWorkitemInstancesRoute, Name = KnownRouteNames.VersionedCancelWorkitemInstance)]
        [Route(KnownRoutes.CancelWorkitemInstancesRoute, Name = KnownRouteNames.CancelWorkitemInstance)]
        [AuditEventType(AuditEventSubType.CancelWorkitem)]
        public async Task<IActionResult> CancelAsync(string workitemInstanceUid = null)
        {
            return await PostCancelAsync(workitemInstanceUid).ConfigureAwait(false);
        }

        private async Task<IActionResult> PostCancelAsync(string workitemInstanceUid)
        {
            long fileSize = Request.ContentLength ?? 0;
            _logger.LogInformation("DICOM Web Store Workitem Transaction request received, with UPS instance UID {UPSInstanceUid}, and file size of {FileSize} bytes",
                workitemInstanceUid ?? string.Empty,
                fileSize);

            var response = await _mediator.CancelWorkitemAsync(
                    Request.Body,
                    Request.ContentType,
                    workitemInstanceUid,
                    HttpContext.RequestAborted)
                .ConfigureAwait(false);

            return StatusCode((int)response.Status.ToHttpStatusCode());
        }
    }
}
