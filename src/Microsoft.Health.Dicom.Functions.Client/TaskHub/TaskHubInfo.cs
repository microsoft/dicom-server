// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Functions.Client.TaskHub;

internal sealed class TaskHubInfo
{
    public string TaskHubName { get; set; }

    public DateTime CreatedAt { get; set; }

    public int PartitionCount { get; set; }
}
