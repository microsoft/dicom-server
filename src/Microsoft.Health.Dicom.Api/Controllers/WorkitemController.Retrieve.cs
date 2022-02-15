// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Api.Features.Routing;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    public partial class WorkitemController
    {
        /// <summary>
        /// This action requests a UPS Instance on the Origin-Server. It corresponds to the UPS DIMSE N-GET operation.
        /// </summary>
        [HttpGet]
        [VersionedPartitionRoute(KnownRoutes.RetrieveWorkitemInstancesRoute, Name = KnownRouteNames.VersionedPartitionRetrieveWorkitemInstance)]
        [PartitionRoute(KnownRoutes.RetrieveWorkitemInstancesRoute, Name = KnownRouteNames.PartitionedRetrieveWorkitemInstance)]
        [VersionedRoute(KnownRoutes.RetrieveWorkitemInstancesRoute, Name = KnownRouteNames.VersionedRetrieveWorkitemInstance)]
        [Route(KnownRoutes.RetrieveWorkitemInstancesRoute, Name = KnownRouteNames.RetrieveWorkitemInstance)]
        public IActionResult RetrieveAsync()
        {
            _logger.LogInformation("Requesting non-implemented endpoint.");
            return new StatusCodeResult((int)HttpStatusCode.NotFound);
        }
    }
}
