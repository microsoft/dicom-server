// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Api.Features.Export;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [ApiVersion("1.0-prerelease")]
    [QueryModelStateValidator]
    [ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
    public class ExportController : Controller
    {
        private readonly IExportService _exportService;
        private readonly ILogger<ExportController> _logger;

        public ExportController(IExportService exportService, ILogger<ExportController> logger)
        {
            _exportService = exportService;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [VersionedRoute("export")]
        [Route("export")]
        [AuditEventType("export")]
        public async Task<IActionResult> Export([FromBody] ExportRequest request)
        {
            await _exportService.Export(
#pragma warning disable CA1062 // Validate arguments of public methods
                            request.Instances,
                            request.CohortId,
#pragma warning restore CA1062 // Validate arguments of public methods
                            request.DestinationBlobConnectionString,
                request.DestinationBlobContainerName,
                cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

            return StatusCode((int)HttpStatusCode.OK);
        }
    }
}
