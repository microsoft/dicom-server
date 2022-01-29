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
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    public partial class WorkitemController
    {
        /// <summary>
        /// This transaction searches the Worklist for Workitems that match the specified Query Parameters and returns a list of matching Workitems.
        /// Each Workitem in the returned list includes return Attributes specified in the request. The transaction corresponds to the UPS DIMSE C-FIND operation.
        /// </summary>
        /// <returns>List of DicomDataset</returns>
        [HttpGet]
        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [Produces(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [VersionedPartitionRoute(KnownRoutes.SearchWorkitemInstancesRoute)]
        [PartitionRoute(KnownRoutes.SearchWorkitemInstancesRoute)]
        [VersionedRoute(KnownRoutes.SearchWorkitemInstancesRoute)]
        [Route(KnownRoutes.SearchWorkitemInstancesRoute)]
        [AuditEventType(AuditEventSubType.Query)]
        public async Task<IActionResult> SearchWorkitemsAsync([FromQuery] QueryOptions options)
        {
            _logger.LogInformation("Workitem search request received. QueryString {RequestQueryString}.", Request.QueryString);

            EnsureArg.IsNotNull(options);
            var response = await _mediator.QueryWorkitemsAsync(
                options.ToParameters(Request.Query, QueryResource.WorkitemInstances),
                cancellationToken: HttpContext.RequestAborted);

            Response.TryAddErroneousAttributesHeader(response.ErroneousTags);
            if (!response.ResponseDataset.Any())
            {
                return NoContent();
            }

            return StatusCode((int)HttpStatusCode.OK, response.ResponseDataset);
        }
    }
}
