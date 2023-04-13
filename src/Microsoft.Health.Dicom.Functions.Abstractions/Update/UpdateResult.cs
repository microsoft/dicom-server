// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Functions.Update;
public class UpdateResult
{
    public int StudyUpdated { get; }

    public long InstanceUpdated { get; }

    public long InstanceFailed { get; }

    public IReadOnlyList<string> Errors { get; }

    public UpdateResult(int studyUpdated, long instanceUpdated, long instanceFailed, IReadOnlyList<string> errors)
    {
        StudyUpdated = studyUpdated;
        InstanceUpdated = instanceUpdated;
        InstanceFailed = instanceFailed;
        Errors = errors;
    }
}
