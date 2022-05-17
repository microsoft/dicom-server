// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
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
    [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson })]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [VersionedPartitionRoute(KnownRoutes.ChangeStateWorkitemInstancesRoute, Name = KnownRouteNames.VersionedPartitionChangeStateWorkitemInstance)]
    [VersionedRoute(KnownRoutes.ChangeStateWorkitemInstancesRoute, Name = KnownRouteNames.VersionedChangeStateWorkitemInstance)]
    [Route(KnownRoutes.ChangeStateWorkitemInstancesRoute, Name = KnownRouteNames.ChangeStateWorkitemInstance)]
    [AuditEventType(AuditEventSubType.ChangeStateWorkitem)]
    public async Task<IActionResult> ChangeStateAsync(string workitemInstanceUid)
    {
        var response = await _mediator
                    .ChangeWorkitemStateAsync(
                        Request.Body,
                        Request.ContentType,
                        workitemInstanceUid,
                        cancellationToken: HttpContext.RequestAborted)
                    .ConfigureAwait(false);

        return StatusCode((int)response.Status.ChangeStateResponseToHttpStatusCode(), response.Message);
    }
}
