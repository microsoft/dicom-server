// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;
using DicomApiAuditLoggingFilterAttribute = Microsoft.Health.Dicom.Api.Features.Audit.AuditLoggingFilterAttribute;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [ServiceFilter(typeof(DicomApiAuditLoggingFilterAttribute))]
    public class OperationsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<OperationsController> _logger;

        public OperationsController(IMediator mediator, ILogger<OperationsController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route(KnownRoutes.OperationInstanceRoute)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(OperationStatusResponse), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType(typeof(OperationStatusResponse), (int)HttpStatusCode.OK)]
        [AuditEventType(AuditEventSubType.Operation)]
        public async Task<IActionResult> GetOperationStatusAsync([Required] string operationId)
        {
            _logger.LogInformation("DICOM Web Get Operation Status request received for ID '{OperationId}'", operationId);

            OperationStatusResponse response = await _mediator.GetOperationStatusAsync(operationId, HttpContext.RequestAborted);

            if (response == null)
            {
                return NotFound();
            }

            HttpStatusCode statusCode;
            if (response.Status == OperationStatus.Pending || response.Status == OperationStatus.Running)
            {
                Response.AddLocationHeader(new Uri(UriHelper.BuildRelative(Request.PathBase, Request.Path), UriKind.Relative));
                statusCode = HttpStatusCode.Accepted;
            }
            else
            {
                statusCode = HttpStatusCode.OK;
            }

            return StatusCode((int)statusCode, response);
        }
    }
}
