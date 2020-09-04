// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.DicomCast.Core.Features.State
{
    /// <summary>
    /// Current state of sync
    /// </summary>
    public class SyncState
    {
        public SyncState(long syncedSequence, DateTimeOffset syncedDate)
        {
            SyncedSequence = syncedSequence;
            SyncedDate = syncedDate;
        }

        /// <summary>
        /// Sequence number of the processed dicom event.
        /// </summary>
        public long SyncedSequence { get; }

        /// <summary>
        /// Server time when the last dicom event was processed.
        /// </summary>
        public DateTimeOffset SyncedDate { get; }

        /// <summary>
        /// Creates the model with initial state before the sync starts
        /// </summary>
        /// <returns>SyncState</returns>
        public static SyncState CreateInitialSyncState()
        {
            return new SyncState(0, DateTime.MinValue);
        }
    }
}
