// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.BulkImport;

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
            string requestContentInString = requestContent?.ToString();

            _logger.LogInformation("Received bulk import event: {RequestContent}.", requestContentInString);

            EventGridEvent[] eventGridEvents = _eventGridSubscriber.DeserializeEventGridEvents(requestContentInString);

            var blobReferences = new List<(string, BlobReference)>(eventGridEvents.Length);

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
                else if (e.Data is StorageBlobCreatedEventData storageBlobCreatedEventData)
                {
                    var uri = new Uri(storageBlobCreatedEventData.Url);

                    var blob = new CloudBlob(uri);

                    blobReferences.Add((uri.Host.Substring(0, uri.Host.IndexOf(".", StringComparison.InvariantCulture)), new BlobReference(blob.Container.Name, blob.Name)));
                }
            }

            foreach (IGrouping<string, BlobReference> grouping in blobReferences.GroupBy(blobReference => blobReference.Item1, blobReference => blobReference.Item2))
            {
                await _mediator.QueueBulkImportEntriesAsync(grouping.Key, grouping.ToArray());
            }

            return StatusCode((int)HttpStatusCode.OK, string.Empty);
        }
    }
}
