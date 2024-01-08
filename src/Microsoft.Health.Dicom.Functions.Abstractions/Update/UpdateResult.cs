// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Functions.Update;

public class UpdateResult
{
    public int StudyProcessed { get; }

    public int StudyUpdated { get; }

    public int StudyFailed { get; }

    public long InstanceUpdated { get; }

    public IReadOnlyList<string> Errors { get; }

    public UpdateResult(int studyProcessed, int studyUpdated, long instanceUpdated, int studyFailed, IReadOnlyList<string> errors)
    {
        StudyProcessed = studyProcessed;
        StudyUpdated = studyUpdated;
        InstanceUpdated = instanceUpdated;
        StudyFailed = studyFailed;
        Errors = errors;
    }
}
