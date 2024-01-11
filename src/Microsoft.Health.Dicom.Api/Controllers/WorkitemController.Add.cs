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
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Messages.Workitem;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Controllers;

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
    [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson })]
    [Produces(KnownContentTypes.ApplicationDicomJson)]
    [Consumes(KnownContentTypes.ApplicationDicomJson)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.UnsupportedMediaType)]
    [HttpPost]
    [VersionedPartitionRoute(KnownRoutes.AddWorkitemInstancesRoute, Name = KnownRouteNames.PartitionedAddWorkitemInstance)]
    [VersionedRoute(KnownRoutes.AddWorkitemInstancesRoute, Name = KnownRouteNames.AddWorkitemInstance)]
    [AuditEventType(AuditEventSubType.AddWorkitem)]
    public async Task<IActionResult> AddAsync([FromBody][Required][MinLength(1)] IReadOnlyList<DicomDataset> dicomDatasets)
    {
        // The Workitem UID is passed as the name of the first query parameter 
        string workitemUid = HttpContext.Request.Query.Keys.FirstOrDefault();

        return await PostAddAsync(workitemUid, dicomDatasets);
    }

    private async Task<IActionResult> PostAddAsync(string workitemInstanceUid, IReadOnlyList<DicomDataset> dicomDatasets)
    {
        _logger.LogInformation("DICOM Web Add Workitem Transaction request received with file size of {FileSize} bytes.", Request.ContentLength);

        AddWorkitemResponse response = await _mediator.AddWorkitemAsync(
            dicomDatasets[0],
            Request.ContentType,
            workitemInstanceUid,
            HttpContext.RequestAborted);

        if (response.Status == WorkitemResponseStatus.Success)
        {
            Response.Headers.Append(HeaderNames.ContentLocation, response.Uri.ToString());
            Response.Headers.Append(HeaderNames.Location, response.Uri.ToString());
        }

        return StatusCode((int)response.Status.AddResponseToHttpStatusCode(), response.Message);
    }
}
