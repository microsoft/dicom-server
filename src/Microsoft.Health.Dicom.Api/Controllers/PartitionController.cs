// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Web;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [ApiVersion("1.0-prerelease")]
    [ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
    public class PartitionController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<QueryController> _logger;
        private readonly bool _featureEnabled;

        public PartitionController(IMediator mediator, ILogger<QueryController> logger, IOptions<FeatureConfiguration> featureConfiguration)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));

            _mediator = mediator;
            _logger = logger;
            _featureEnabled = featureConfiguration.Value.EnableDataPartitions;
        }

        [HttpGet]
        [Produces(KnownContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(IEnumerable<PartitionEntry>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [VersionedRoute(KnownRoutes.GetAllPartitionsRoute)]
        [Route(KnownRoutes.GetAllPartitionsRoute)]
        [AuditEventType(AuditEventSubType.Partition)]
        public async Task<IActionResult> GetAllPartitions()
        {
            if (!_featureEnabled)
            {
                throw new DataPartitionsFeatureDisabledException();
            }

            _logger.LogInformation("DICOM Web Get partitions request received to get all partitions");

            var response = await _mediator.GetPartitionsAsync(cancellationToken: HttpContext.RequestAborted);

            return StatusCode((int)HttpStatusCode.OK, response.Entries);
        }
    }
}
