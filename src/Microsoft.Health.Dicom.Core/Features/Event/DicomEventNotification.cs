// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Event
{
    public class DicomEventNotification
    {
        public DicomEventNotification(DicomEventType eventType, object eventData, string eventSubject)
        {
            EnsureArg.IsNotNull(eventData);

            EventType = eventType;
            EventData = eventData;
            EventSubject = eventSubject;
        }

        public DicomEventType EventType { get; }

        public object EventData { get; }

        public string EventSubject { get; }
    }
}
