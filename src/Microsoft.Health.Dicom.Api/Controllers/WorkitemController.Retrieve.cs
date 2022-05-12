// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Messages.Workitem;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Api.Controllers;

public partial class WorkitemController
{
    /// <summary>
    /// This action requests a UPS Instance on the Origin-Server. It corresponds to the UPS DIMSE N-GET operation.
    /// </summary>
    [HttpGet]
    [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson })]
    [Produces(KnownContentTypes.ApplicationDicomJson)]
    [ProducesResponseType(typeof(DicomDataset), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [PartitionRoute(KnownRoutes.RetrieveWorkitemInstancesRoute, Name = KnownRouteNames.PartitionedRetrieveWorkitemInstance)]
    [Route(KnownRoutes.RetrieveWorkitemInstancesRoute, Name = KnownRouteNames.RetrieveWorkitemInstance)]
    [AuditEventType(AuditEventSubType.RetrieveWorkitem)]
    public async Task<IActionResult> RetrieveAsync(string workitemInstanceUid)
    {
        var response = await _mediator
            .RetrieveWorkitemAsync(workitemInstanceUid, cancellationToken: HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (response.Status == WorkitemResponseStatus.Success)
        {
            return Ok(response.Dataset);
        }

        return StatusCode((int)response.Status.RetrieveResponseToHttpStatusCode(), response.Message);
    }
}
