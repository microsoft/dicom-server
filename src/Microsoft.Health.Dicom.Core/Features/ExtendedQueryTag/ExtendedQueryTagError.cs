// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Represents each Extended Query Tag Error that will be surfaced to the user.
    /// </summary>
    public class ExtendedQueryTagError
    {
        public ExtendedQueryTagError(DateTime timestamp,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            string errorMessage)
        {
            StudyInstanceUid = EnsureArg.IsNotNull(studyInstanceUid); ;
            SeriesInstanceUid = seriesInstanceUid;
            SopInstanceUid = sopInstanceUid;
            UtcTimestamp = timestamp;
            ErrorMessage = EnsureArg.IsNotNullOrWhiteSpace(errorMessage); ;
        }

        public string StudyInstanceUid { get; }

        public string SeriesInstanceUid { get; }

        public string SopInstanceUid { get; }

        public DateTime UtcTimestamp { get; }

        public string ErrorMessage { get; }
    }
}
