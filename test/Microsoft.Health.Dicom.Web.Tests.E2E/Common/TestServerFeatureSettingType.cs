// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common;

/// <summary>
/// Flags Used to Enable/Disable specific feature setting in the local server app configuration for e2e tests.
/// Use only the powers of 2 for member values, to make use of the "flag" behavior.
/// </summary>
[Flags]
public enum TestServerFeatureSettingType : byte
{
    // Default
    None = 0,

    // Enable Data Partition
    DataPartition = 1,
    EnableLatestApiVersion = 2
}
