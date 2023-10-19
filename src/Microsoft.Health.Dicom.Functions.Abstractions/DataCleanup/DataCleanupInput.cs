// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Functions.DataCleanup;

public class DataCleanupInput
{
    public BatchingOptions Batching { get; set; }

    public DateTimeOffset StartFilterTimeStamp { get; set; }

    public DateTimeOffset EndFilterTimeStamp { get; set; }
}
