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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;
using DicomApiAuditLoggingFilterAttribute = Microsoft.Health.Dicom.Api.Features.Audit.AuditLoggingFilterAttribute;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    /// <summary>
    /// Represents the REST API controller for interacting with long-running DICOM operations.
    /// </summary>
    [ServiceFilter(typeof(DicomApiAuditLoggingFilterAttribute))]
    public class OperationsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUrlResolver _urlResolver;
        private readonly ILogger<OperationsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationsController"/> class.
        /// </summary>
        /// <param name="mediator">A mediator object for passing requests to corresponding handlers.</param>
        /// <param name="urlResolver">A helper for building URLs within the application.</param>
        /// <param name="logger">A controller-specific logger.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="mediator"/> or <paramref name="logger"/> is <see langword="null"/>.
        /// </exception>
        public OperationsController(
            IMediator mediator,
            IUrlResolver urlResolver,
            ILogger<OperationsController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(urlResolver, nameof(urlResolver));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _urlResolver = urlResolver;
            _logger = logger;
        }

        /// <summary>
        /// Gets the status of a DICOM operation based on its ID.
        /// </summary>
        /// <remarks>
        /// If the operation has not yet completed, then its response will include a "Location" header directing
        /// clients to the URL where the status can be checked queried.
        /// </remarks>
        /// <param name="operationId">The unique ID for a particular DICOM operation.</param>
        /// <returns>
        /// A task representing the <see cref="GetStatusAsync(string)"/> operation. The value of its
        /// <see cref="Task{TResult}.Result"/> property contains the status of the operation, if found;
        /// otherwise <see cref="NotFoundResult"/>
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="operationId"/> consists of white space characters.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="operationId"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException">The connection was aborted.</exception>
        [HttpGet]
        [Route(KnownRoutes.OperationInstanceRoute)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(OperationStatusResponse), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType(typeof(OperationStatusResponse), (int)HttpStatusCode.OK)]
        [AuditEventType(AuditEventSubType.Operation)]
        public async Task<IActionResult> GetStatusAsync([Required] string operationId)
        {
            _logger.LogInformation("DICOM Web Get Operation Status request received for ID '{OperationId}'", operationId);

            OperationStatusResponse response = await _mediator.GetOperationStatusAsync(operationId, HttpContext.RequestAborted);

            if (response == null)
            {
                return NotFound();
            }

            HttpStatusCode statusCode;
            if (response.Status == OperationRuntimeStatus.Pending || response.Status == OperationRuntimeStatus.Running)
            {
                Response.AddLocationHeader(_urlResolver.ResolveOperationStatusUri(operationId));
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
