// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Features.Store;

[Flags]
public enum ValidationWarnings
{
    None = 0,

    IndexedDicomTagHasMultipleValues = 1,

    DatasetDoesNotMatchSOPClass = 2,
}
