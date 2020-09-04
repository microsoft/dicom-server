// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.DicomCast.Core.UnitTests
{
    public static class ChangeFeedGenerator
    {
        public static ChangeFeedEntry Generate(long? sequence = null, ChangeFeedAction? action = null, string studyInstanceUid = null, string seriesInstanceUid = null, string sopInstanceUid = null, ChangeFeedState? state = null, DicomDataset metadata = null)
        {
            if (sequence == null)
            {
                sequence = 1;
            }

            if (action == null)
            {
                action = ChangeFeedAction.Create;
            }

            if (string.IsNullOrEmpty(studyInstanceUid))
            {
                studyInstanceUid = DicomUID.Generate().UID;
            }

            if (string.IsNullOrEmpty(seriesInstanceUid))
            {
                seriesInstanceUid = DicomUID.Generate().UID;
            }

            if (string.IsNullOrEmpty(sopInstanceUid))
            {
                sopInstanceUid = DicomUID.Generate().UID;
            }

            if (state == null)
            {
                state = ChangeFeedState.Current;
            }

            var changeFeedEntry = new ChangeFeedEntry(sequence.Value, DateTime.UtcNow, action.Value, studyInstanceUid, seriesInstanceUid, sopInstanceUid, state.Value)
            {
                Metadata = metadata,
            };

            return changeFeedEntry;
        }
    }
}
