// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Core.Configs;

public class HealthCheckPublisherConfiguration
{
    public const string SectionName = "HealthCheckPublisher";

    public IEnumerable<string> ExcludedHealthCheckNames { get; set; } = new List<string>();
}
