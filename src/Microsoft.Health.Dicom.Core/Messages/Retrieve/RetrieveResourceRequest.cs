// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve;

public class RetrieveResourceRequest : IRequest<RetrieveResourceResponse>
{
    public RetrieveResourceRequest(string studyInstanceUid, IReadOnlyCollection<AcceptHeader> acceptHeaders, bool isOriginalVersionRequested = false)
        : this(ResourceType.Study, acceptHeaders, isOriginalVersionRequested)
    {
        StudyInstanceUid = studyInstanceUid;
    }

    public RetrieveResourceRequest(string studyInstanceUid, string seriesInstanceUid, IReadOnlyCollection<AcceptHeader> acceptHeaders, bool isOriginalVersionRequested = false)
        : this(ResourceType.Series, acceptHeaders, isOriginalVersionRequested)
    {
        StudyInstanceUid = studyInstanceUid;
        SeriesInstanceUid = seriesInstanceUid;
    }

    public RetrieveResourceRequest(
         string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, IReadOnlyCollection<AcceptHeader> acceptHeaders, bool isOriginalVersionRequested = false)
        : this(ResourceType.Instance, acceptHeaders, isOriginalVersionRequested)
    {
        StudyInstanceUid = studyInstanceUid;
        SeriesInstanceUid = seriesInstanceUid;
        SopInstanceUid = sopInstanceUid;
    }

    public RetrieveResourceRequest(
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid,
        IReadOnlyCollection<int> frames,
        IReadOnlyCollection<AcceptHeader> acceptHeaders,
        bool isOriginalVersionRequested = false)
        : this(ResourceType.Frames, acceptHeaders, isOriginalVersionRequested)
    {
        StudyInstanceUid = studyInstanceUid;
        SeriesInstanceUid = seriesInstanceUid;
        SopInstanceUid = sopInstanceUid;

        // Per DICOMWeb spec (http://dicom.nema.org/medical/dicom/current/output/html/part18.html#sect_9.5.1.2.1)
        // frame number in the URI is 1-based, unlike fo-dicom representation where it's 0-based.
        Frames = frames?.Select(x => x - 1).ToList();
    }

    private RetrieveResourceRequest(ResourceType resourceType, IReadOnlyCollection<AcceptHeader> acceptHeaders, bool isOriginalVersionRequested)
    {
        ResourceType = resourceType;
        AcceptHeaders = acceptHeaders;
        IsOriginalVersionRequested = isOriginalVersionRequested;
    }

    public ResourceType ResourceType { get; }

    public string StudyInstanceUid { get; }

    public string SeriesInstanceUid { get; }

    public string SopInstanceUid { get; }

    public IReadOnlyCollection<int> Frames { get; }

    public IReadOnlyCollection<AcceptHeader> AcceptHeaders { get; }

    public bool IsOriginalVersionRequested { get; }
}
