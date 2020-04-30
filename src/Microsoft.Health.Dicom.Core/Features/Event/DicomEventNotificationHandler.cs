// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;

namespace Microsoft.Health.Dicom.Core.Features.Event
{
    public class DicomEventNotificationHandler : INotificationHandler<DicomEventNotificationCollection>
    {
        private readonly DicomEventingService _eventService;

        public DicomEventNotificationHandler(DicomEventingService eventService)
        {
            EnsureArg.IsNotNull(eventService, nameof(eventService));

            _eventService = eventService;
        }

        public async Task Handle(DicomEventNotificationCollection events, CancellationToken cancellationToken)
        {
            await _eventService.PublishEventAsync(events, cancellationToken);
        }
    }
}
