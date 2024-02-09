// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Configs;

/// <summary>
/// Configuration for extended query tag feature.
/// </summary>
public class ExtendedQueryTagConfiguration
{
    /// <summary>
    /// Maximum allowed number of tags.
    /// </summary>
    public int MaxAllowedCount { get; set; } = 128;

    public int OperationRetryCount { get; set; } = 90;

    public TimeSpan OperationRetryInterval { get; set; } = TimeSpan.FromSeconds(10);
}
