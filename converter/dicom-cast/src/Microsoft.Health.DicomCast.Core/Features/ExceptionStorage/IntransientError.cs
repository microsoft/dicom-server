// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;

public class IntransientError
{
    public IntransientError()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntransientError"/> class.
    /// </summary>
    /// <param name="studyUid">StudyUid of the changefeed entry that failed </param>
    /// <param name="seriesUid">SeriesUid of the changefeed entry that failed</param>
    /// <param name="instanceUid">InstanceUid of the changefeed entry that failed</param>
    /// <param name="changeFeedSequence">Changefeed sequence number that threw exception</param>
    /// <param name="ex">The exception that was thrown</param>
    public IntransientError(string studyUid, string seriesUid, string instanceUid, long changeFeedSequence, Exception ex)
    {
        EnsureArg.IsNotNull(studyUid, nameof(studyUid));
        EnsureArg.IsNotNull(seriesUid, nameof(seriesUid));
        EnsureArg.IsNotNull(instanceUid, nameof(instanceUid));
        EnsureArg.IsNotNull(ex, nameof(ex));

        StudyUid = studyUid;
        SeriesUid = seriesUid;
        InstanceUid = instanceUid;
        ChangeFeedSequence = changeFeedSequence;
        Exception = ex.ToString();
    }

    public string StudyUid { get; set; }

    public string SeriesUid { get; set; }

    public string InstanceUid { get; set; }

    public string Exception { get; set; }

    public long ChangeFeedSequence { get; set; }
}
