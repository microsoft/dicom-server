// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    public class BulkImportController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly EventGridSubscriber _eventGridSubscriber = new EventGridSubscriber();

        public BulkImportController(
            IMediator mediator,
            ILogger<BulkImportController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [Route("bulkImport/{storageAccountName}")]
        public async Task<IActionResult> EnableBulkImportSource(string storageAccountName)
        {
            _logger.LogInformation("Received bulk import.");

            await _mediator.EnableBulkImportSourceAsync(storageAccountName, HttpContext.RequestAborted);

            return StatusCode((int)HttpStatusCode.OK, string.Empty);
        }

        [HttpPost]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [Route("webhooks/bulkImport")]
        public async Task<IActionResult> BulkImportNotification([FromBody]object requestContent)
        {
            _logger.LogInformation("Received bulk import event.");

            await Task.Delay(0);

            EventGridEvent[] eventGridEvents = _eventGridSubscriber.DeserializeEventGridEvents(requestContent.ToString());

            foreach (EventGridEvent e in eventGridEvents)
            {
                if (e.Data is SubscriptionValidationEventData sved)
                {
                    _logger.LogInformation($"Got validation event: {sved.ValidationCode}.");

                    var responseData = new SubscriptionValidationResponse()
                    {
                        ValidationResponse = sved.ValidationCode,
                    };

                    return StatusCode((int)HttpStatusCode.OK, responseData);
                }
            }

            return StatusCode((int)HttpStatusCode.OK, string.Empty);
        }
    }
}
