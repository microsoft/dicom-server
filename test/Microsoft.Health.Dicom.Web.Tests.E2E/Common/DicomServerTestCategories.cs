// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common;

[Flags]
public enum DicomTestServerCategory : byte
{
    None,
    Features,
    DataPartition,
    DualWrite
}
