// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Messages.Workitem;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Controllers;

public partial class WorkitemController
{
    /// <summary>
    /// This action requests the update of a UPS Instance on the Origin-Server. It corresponds to the UPS DIMSE N-SET operation.
    /// </summary>
    /// <remarks>
    /// The request body contains all the metadata to be updated in DICOM PS 3.18 JSON metadata.
    /// </remarks>
    /// <returns></returns>
    [HttpPost]
    [Consumes(KnownContentTypes.ApplicationDicomJson)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.UnsupportedMediaType)]
    [VersionedPartitionRoute(KnownRoutes.UpdateWorkitemInstancesRoute, Name = KnownRouteNames.PartitionedUpdateWorkitemInstance)]
    [VersionedRoute(KnownRoutes.UpdateWorkitemInstancesRoute, Name = KnownRouteNames.UpdateWorkitemInstance)]
    [AuditEventType(AuditEventSubType.UpdateWorkitem)]
    public async Task<IActionResult> UpdateAsync(string workitemInstanceUid, [FromBody][Required][MinLength(1)] IReadOnlyList<DicomDataset> dicomDatasets)
    {
        // The Transaction UID is passed as the first query parameter 
        string transactionUid = HttpContext.Request.Query.Keys.FirstOrDefault();

        return await PostUpdateAsync(workitemInstanceUid, transactionUid, dicomDatasets);
    }

    private async Task<IActionResult> PostUpdateAsync(string workitemInstanceUid, string transactionUid, IReadOnlyList<DicomDataset> dicomDatasets)
    {
        _logger.LogInformation("DICOM Web Update Workitem Transaction request received with file size of {FileSize} bytes.", Request.ContentLength);

        UpdateWorkitemResponse response = await _mediator.UpdateWorkitemAsync(
            dicomDatasets[0],
            Request.ContentType,
            workitemInstanceUid,
            transactionUid,
            HttpContext.RequestAborted);

        if (response.Status == WorkitemResponseStatus.Success)
        {
            Response.Headers.Append(HeaderNames.ContentLocation, response.Uri.ToString());
        }

        if (!string.IsNullOrWhiteSpace(response.Message))
        {
            Response.SetWarning(HttpWarningCode.MiscPersistentWarning, Request.GetHost(dicomStandards: true), response.Message);
        }

        return StatusCode((int)response.Status.UpdateResponseToHttpStatusCode(), response.Message);
    }
}
