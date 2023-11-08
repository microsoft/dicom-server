// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Polly;

namespace Microsoft.Health.Dicom.Api.Configs;

public class DeleteWorkerOptions
{
    public DelayBackoffType BackoffType { get; set; } = DelayBackoffType.Exponential;

    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(5);

    public int MaxRetryAttempts { get; set; } = 5;

    public bool UseJitter { get; set; } = true;
}
