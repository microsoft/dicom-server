// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Queue.Features.Messages
{
    internal class DicomQueueMessage
    {
        private readonly string _studyInstanceUID;
        private readonly string _seriesInstanceUID;
        private readonly string _sopInstanceUID;

        public DicomQueueMessage(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID)
        {
            _studyInstanceUID = studyInstanceUID;
            _seriesInstanceUID = seriesInstanceUID;
            _sopInstanceUID = sopInstanceUID;

            MessageId = Guid.NewGuid().ToString();
        }

        public string MessageId { get; }
    }
}
