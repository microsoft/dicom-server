// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common;

/// <summary>
/// 
/// </summary>
[Flags]
public enum TestServerFeatureSettingType : byte
{
    // Default
    None,

    // Enable UPS-RS
    UpsRs,

    // Enable Data Partition
    DataPartition,

    // Enable Dual Write
    DualWrite
}
