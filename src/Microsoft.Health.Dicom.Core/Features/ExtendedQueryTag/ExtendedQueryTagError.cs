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
        public ExtendedQueryTagError(
            DateTime createdTime,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            string errorMessage,
            ExtendedQueryTagErrorStatus status,
            DateTime acknowledgedTime)
        {
            StudyInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid);
            SeriesInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid);
            SopInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid);
            CreatedTime = createdTime;
            ErrorMessage = EnsureArg.IsNotNullOrWhiteSpace(errorMessage);
            Status = status;
            AcknowledgedTime = acknowledgedTime;
        }

        public string StudyInstanceUid { get; }

        public string SeriesInstanceUid { get; }

        public string SopInstanceUid { get; }

        public DateTime CreatedTime { get; }

        public string ErrorMessage { get; }

        public ExtendedQueryTagErrorStatus Status { get; }

        public DateTime? AcknowledgedTime { get; }

    }
}
