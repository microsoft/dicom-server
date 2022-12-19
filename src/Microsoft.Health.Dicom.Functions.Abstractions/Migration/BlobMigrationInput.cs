// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Functions.Migration;

public class BlobMigrationInput
{
    public BatchingOptions Batching { get; set; }

    public DateTime? FilterTimeStamp { get; set; }
}
