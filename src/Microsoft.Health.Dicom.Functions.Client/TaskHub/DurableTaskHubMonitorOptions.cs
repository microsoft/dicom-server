// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Functions.Client.TaskHub;

internal class DurableTaskHubMonitorOptions
{
    public const string SectionName = "Monitor";

    public bool Enabled { get; set; }

    [Range(typeof(TimeSpan), "00:00:00", "00:05:00", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true)]
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(10);
}
