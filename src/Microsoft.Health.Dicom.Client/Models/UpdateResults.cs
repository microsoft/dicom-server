// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Client.Models;

public class UpdateResults
{
    public int StudyProcessed { get; set; }

    public int StudyUpdated { get; set; }

    public int StudyFailed { get; set; }

    public long InstanceUpdated { get; set; }

    public IReadOnlyList<string> Errors { get; set; }
}
