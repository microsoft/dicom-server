// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using FellowOakDicom;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers;

public partial class WorkitemController
{
    /// <summary>
    /// This action requests a UPS Instance on the Origin-Server. It corresponds to the UPS DIMSE N-GET operation.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(DicomDataset), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    [VersionedPartitionRoute(KnownRoutes.ChangeStateWorkitemInstancesRoute, Name = KnownRouteNames.VersionedPartitionChangeStateWorkitemInstance)]
    [PartitionRoute(KnownRoutes.ChangeStateWorkitemInstancesRoute, Name = KnownRouteNames.PartitionedChangeStateWorkitemInstance)]
    [VersionedRoute(KnownRoutes.ChangeStateWorkitemInstancesRoute, Name = KnownRouteNames.VersionedChangeStateWorkitemInstance)]
    [Route(KnownRoutes.ChangeStateWorkitemInstancesRoute, Name = KnownRouteNames.ChangeStateWorkitemInstance)]
    [AuditEventType(AuditEventSubType.ChangeStateWorkitem)]
    public IActionResult ChangeStateAsync(string workitemInstanceUid, string workitemState)
    {
        _logger.LogInformation("Change workitem state to {WorkitemState}.", workitemState);

        return new StatusCodeResult((int)HttpStatusCode.NotFound);
    }
}
