// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Functions.Migration;

public class MigratingFilesInput
{
    public BatchingOptions Batching { get; set; }

    public DateTime StartFilterTimeStamp { get; set; }

    public DateTime EndFilterTimeStamp { get; set; }
}
