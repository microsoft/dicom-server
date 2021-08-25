// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag
{
    public class AcknowledgeTagErrorRequest : IRequest<AcknowledgeTagErrorResponse>
    {
        public AcknowledgeTagErrorRequest(
            string extendedQueryTagPath,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid)
        {
            ExtendedQueryTagPath = extendedQueryTagPath;
            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
            SopInstanceUid = sopInstanceUid;
        }

        /// <summary>
        /// Path for the extended query tag that is requested.
        /// </summary>
        public string ExtendedQueryTagPath { get; }

        /// <summary>
        /// Study instance uid that is requested.
        /// </summary>
        public string StudyInstanceUid { get; }

        /// <summary>
        /// Series instance uid that is requested.
        /// </summary>
        public string SeriesInstanceUid { get; }

        /// <summary>
        /// Sop instance uid that is requested.
        /// </summary>
        public string SopInstanceUid { get; }
    }
}
