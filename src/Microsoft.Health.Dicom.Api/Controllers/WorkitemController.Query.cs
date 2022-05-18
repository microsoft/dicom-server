// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Api.Models;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Api.Controllers;

public partial class WorkitemController
{
    /// <summary>
    /// This transaction searches the Worklist for Workitems that match the specified Query Parameters and returns a list of matching Workitems.
    /// Each Workitem in the returned list includes return Attributes specified in the request. The transaction corresponds to the UPS DIMSE C-FIND operation.
    /// </summary>
    /// <returns>ObjectResult which contains list of dicomdataset</returns>
    [HttpGet]
    [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson })]
    [Produces(KnownContentTypes.ApplicationDicomJson)]
    [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [VersionedPartitionRoute(KnownRoutes.SearchWorkitemInstancesRoute)]
    [VersionedRoute(KnownRoutes.SearchWorkitemInstancesRoute)]
    [AuditEventType(AuditEventSubType.QueryWorkitem)]
    [QueryModelStateValidator]
    public async Task<IActionResult> QueryWorkitemsAsync([FromQuery] QueryOptions options)
    {
        _logger.LogInformation("Query workitem request received.");

        EnsureArg.IsNotNull(options);
        var response = await _mediator.QueryWorkitemsAsync(
            options.ToBaseQueryParameters(Request.Query),
            cancellationToken: HttpContext.RequestAborted);

        return response.ResponseDatasets.Any() ? StatusCode((int)response.Status.QueryResponseToHttpStatusCode(), response.ResponseDatasets) : NoContent();
    }
}
