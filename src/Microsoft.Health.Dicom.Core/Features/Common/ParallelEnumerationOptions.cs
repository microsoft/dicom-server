// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Common;

internal sealed class ParallelEnumerationOptions
{
    public int MaxDegreeOfParallelism { get; init; } = -1;

    public TaskScheduler TaskScheduler { get; init; }
}
