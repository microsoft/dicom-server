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
            string partitionName = default)
        {
            StudyInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid);
            SeriesInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid);
            SopInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid);
            CreatedTime = createdTime;
            ErrorMessage = EnsureArg.IsNotNullOrWhiteSpace(errorMessage);
            PartitionName = partitionName;
        }

        public string PartitionName { get; }

        public string StudyInstanceUid { get; }

        public string SeriesInstanceUid { get; }

        public string SopInstanceUid { get; }

        public DateTime CreatedTime { get; }

        public string ErrorMessage { get; }
    }
}
