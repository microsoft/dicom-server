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
    /// This transaction is used to change the state of a Workitem.
    /// It corresponds to the UPS DIMSE N-ACTION operation "Change UPS State".
    /// State changes are used to claim ownership, complete, or cancel a Workitem.
    /// </summary>
    [HttpPut]
    [Consumes(KnownContentTypes.ApplicationDicomJson)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    [VersionedPartitionRoute(KnownRoutes.ChangeStateWorkitemInstancesRoute, Name = KnownRouteNames.PartitionChangeStateWorkitemInstance)]
    [VersionedRoute(KnownRoutes.ChangeStateWorkitemInstancesRoute, Name = KnownRouteNames.ChangeStateWorkitemInstance)]
    [AuditEventType(AuditEventSubType.ChangeStateWorkitem)]
    public async Task<IActionResult> ChangeStateAsync(string workitemInstanceUid, [FromBody][Required][MinLength(1)][MaxLength(1)] IReadOnlyList<DicomDataset> dicomDatasets)
    {
        var response = await _mediator
                    .ChangeWorkitemStateAsync(
                        dicomDatasets[0],
                        Request.ContentType,
                        workitemInstanceUid,
                        cancellationToken: HttpContext.RequestAborted)
                    .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(response.Message))
        {
            Response.SetWarning(HttpWarningCode.MiscPersistentWarning, Request.GetHost(dicomStandards: true), response.Message);
        }

        return StatusCode((int)response.Status.ChangeStateResponseToHttpStatusCode(), response.Message);
    }
}
