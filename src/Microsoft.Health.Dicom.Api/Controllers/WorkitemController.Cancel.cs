// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Api.Controllers;

public partial class WorkitemController
{
    /// <summary>
    /// RequestUPSCancellation
    /// This action requests the cancellation of a UPS Instance managed by the Origin-Server.
    /// It corresponds to the UPS DIMSE N-ACTION operation "Request UPS Cancel".
    /// </summary>
    /// <remarks>
    /// This resource records a request that the specified UPS Instance be canceled.
    /// 
    /// This transaction allows a user agent that does not own a Workitem to request that it be canceled.
    /// It corresponds to the UPS DIMSE N-ACTION operation "Request UPS Cancel". See Section CC.2.2 in PS3.4 .
    /// 
    /// To cancel a Workitem that the user agent owns, i.e., that is in the IN PROGRESS state,
    /// the user agent uses the Change Workitem State transaction as described in Section 11.7.
    /// 
    /// </remarks>
    /// <param name="workitemInstanceUid">The workitem Uid</param>
    /// <param name="dicomDatasets">The DICOM dataset payload in the body.</param>
    /// <returns>Returns a string status report.</returns>
    [HttpPost]
    [Produces(KnownContentTypes.ApplicationDicomJson)]
    [Consumes(KnownContentTypes.ApplicationDicomJson)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Accepted)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Conflict)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.UnsupportedMediaType)]
    [VersionedPartitionRoute(KnownRoutes.CancelWorkitemInstancesRoute, Name = KnownRouteNames.PartitionedCancelWorkitemInstance)]
    [VersionedRoute(KnownRoutes.CancelWorkitemInstancesRoute, Name = KnownRouteNames.CancelWorkitemInstance)]
    [AuditEventType(AuditEventSubType.CancelWorkitem)]
    public async Task<IActionResult> CancelAsync(string workitemInstanceUid, [FromBody][Required][MinLength(1)][MaxLength(1)] IReadOnlyList<DicomDataset> dicomDatasets)
    {
        _logger.LogInformation("DICOM Web Cancel Workitem Transaction request received, with Workitem instance UID {WorkitemInstanceUid}",
            workitemInstanceUid);

        var response = await _mediator.CancelWorkitemAsync(
                dicomDatasets[0],
                Request.ContentType,
                workitemInstanceUid,
                HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (response.Status is Core.Messages.Workitem.WorkitemResponseStatus.Conflict
            && !string.IsNullOrEmpty(response.Message))
        {
            Response.SetWarning(HttpWarningCode.MiscPersistentWarning, Request.GetHost(dicomStandards: true), response.Message);
        }

        return StatusCode((int)response.Status.CancelResponseToHttpStatusCode(), response.Message);
    }

}
