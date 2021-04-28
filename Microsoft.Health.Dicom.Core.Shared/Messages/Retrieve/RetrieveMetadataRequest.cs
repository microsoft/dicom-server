// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve
{
    public class RetrieveMetadataRequest : IRequest<RetrieveMetadataResponse>
    {
        public RetrieveMetadataRequest(string studyInstanceUid, string ifNoneMatch)
        {
            StudyInstanceUid = studyInstanceUid;
            ResourceType = ResourceType.Study;
            IfNoneMatch = ifNoneMatch;
        }

        public RetrieveMetadataRequest(string studyInstanceUid, string seriesInstanceUid, string ifNoneMatch)
        {
            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
            ResourceType = ResourceType.Series;
            IfNoneMatch = ifNoneMatch;
        }

        public RetrieveMetadataRequest(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string ifNoneMatch)
        {
            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
            SopInstanceUid = sopInstanceUid;
            ResourceType = ResourceType.Instance;
            IfNoneMatch = ifNoneMatch;
        }

        public ResourceType ResourceType { get; }

        public string StudyInstanceUid { get; }

        public string SeriesInstanceUid { get; }

        public string SopInstanceUid { get; }

        public string IfNoneMatch { get; }
    }
}
