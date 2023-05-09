// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve;

public class RetrieveMetadataRequest : IRequest<RetrieveMetadataResponse>
{
    public RetrieveMetadataRequest(string studyInstanceUid, string ifNoneMatch, bool isOriginalVersionRequested = false)
    {
        StudyInstanceUid = studyInstanceUid;
        ResourceType = ResourceType.Study;
        IfNoneMatch = ifNoneMatch;
        IsOriginalVersionRequested = isOriginalVersionRequested;
    }

    public RetrieveMetadataRequest(string studyInstanceUid, string seriesInstanceUid, string ifNoneMatch, bool isOriginalVersionRequested = false)
    {
        StudyInstanceUid = studyInstanceUid;
        SeriesInstanceUid = seriesInstanceUid;
        ResourceType = ResourceType.Series;
        IfNoneMatch = ifNoneMatch;
        IsOriginalVersionRequested = isOriginalVersionRequested;
    }

    public RetrieveMetadataRequest(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string ifNoneMatch, bool isOriginalVersionRequested = false)
    {
        StudyInstanceUid = studyInstanceUid;
        SeriesInstanceUid = seriesInstanceUid;
        SopInstanceUid = sopInstanceUid;
        ResourceType = ResourceType.Instance;
        IfNoneMatch = ifNoneMatch;
        IsOriginalVersionRequested = isOriginalVersionRequested;
    }

    public ResourceType ResourceType { get; }

    public string StudyInstanceUid { get; }

    public string SeriesInstanceUid { get; }

    public string SopInstanceUid { get; }

    public string IfNoneMatch { get; }

    public bool IsOriginalVersionRequested { get; }
}
