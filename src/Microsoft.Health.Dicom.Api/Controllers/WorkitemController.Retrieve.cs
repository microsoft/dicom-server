// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Messages.Workitem;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    public partial class WorkitemController
    {
        /// <summary>
        /// This action requests a UPS Instance on the Origin-Server. It corresponds to the UPS DIMSE N-GET operation.
        /// </summary>
        [HttpGet]
        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson }, allowSingle: true, allowMultiple: false)]
        [Produces(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(DicomDataset), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [VersionedPartitionRoute(KnownRoutes.RetrieveWorkitemInstancesRoute, Name = KnownRouteNames.VersionedPartitionRetrieveWorkitemInstance)]
        [PartitionRoute(KnownRoutes.RetrieveWorkitemInstancesRoute, Name = KnownRouteNames.PartitionedRetrieveWorkitemInstance)]
        [VersionedRoute(KnownRoutes.RetrieveWorkitemInstancesRoute, Name = KnownRouteNames.VersionedRetrieveWorkitemInstance)]
        [Route(KnownRoutes.RetrieveWorkitemInstancesRoute, Name = KnownRouteNames.RetrieveWorkitemInstance)]
        [AuditEventType(AuditEventSubType.RetrieveWorkitem)]
        public async Task<IActionResult> RetrieveAsync(string workitemInstanceUid)
        {
            _logger.LogInformation("Search workitem for WorkitemInstanceUid: '{WorkitemInstanceUid}'.", workitemInstanceUid);

            var response = await _mediator
                .RetrieveWorkitemAsync(workitemInstanceUid, cancellationToken: HttpContext.RequestAborted)
                .ConfigureAwait(false);

            // TODO: Need to revisit this
            return response.Status == WorkitemResponseStatus.Success
                ? Ok(response.Dataset)
                : NoContent();
        }
    }
}
