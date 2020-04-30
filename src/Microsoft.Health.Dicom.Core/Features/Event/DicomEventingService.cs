// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;

namespace Microsoft.Health.Dicom.Core.Features.Event
{
    public class DicomEventingService
    {
        private readonly IOptions<EventingConfiguration> _eventConfiguration;
        private readonly ILogger<DicomEventingService> _logger;

        public DicomEventingService(
            IOptions<EventingConfiguration> eventConfiguration,
            ILogger<DicomEventingService> logger)
        {
            _eventConfiguration = eventConfiguration;
            _logger = logger;
        }

        public async Task PublishEventAsync(DicomEventNotificationCollection events, CancellationToken cancellationToken)
        {
            if (!_eventConfiguration.Value.Enabled)
            {
                return;
            }

            try
            {
                string topicHostname = new Uri(_eventConfiguration.Value.TopicEndpoint).Host;
                TopicCredentials topicCredentials = new TopicCredentials(_eventConfiguration.Value.TopicKey);
                var client = new EventGridClient(topicCredentials);

                await client.PublishEventsAsync(topicHostname, GetEventsList(events), cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to publish the event");
            }
        }

        private IList<EventGridEvent> GetEventsList(DicomEventNotificationCollection events)
        {
            return events.EventNotifications.Select(e => new EventGridEvent()
            {
                Id = Guid.NewGuid().ToString(),
                EventType = e.EventType.ToString(),
                Data = e.EventData,
                EventTime = DateTime.Now,
                Subject = e.EventSubject,
                DataVersion = "1.0",
            }).ToList();
        }
    }
}
