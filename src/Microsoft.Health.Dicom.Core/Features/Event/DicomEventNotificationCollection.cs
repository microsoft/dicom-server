// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using MediatR;

namespace Microsoft.Health.Dicom.Core.Features.Event
{
    public class DicomEventNotificationCollection : INotification
    {
        public DicomEventNotificationCollection(IReadOnlyCollection<DicomEventNotification> eventNotifications)
        {
            EventNotifications = eventNotifications;
        }

        public IReadOnlyCollection<DicomEventNotification> EventNotifications { get; }
    }
}
