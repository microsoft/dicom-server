// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Core.Configs;

public class HealthCheckPublisherConfiguration
{
    public const string SectionName = "HealthCheckPublisher";

    /// <summary>
    /// A comma separated list of health check names to exclude.
    /// Example: "DcmHealthCheck,MetadataHealthCheck"
    /// </summary>
    public string ExcludedHealthCheckNames { get; set; }

    public IReadOnlyList<string> GetListOfExcludedHealthCheckNames()
    {
        return ExcludedHealthCheckNames?.Split(',') ?? Array.Empty<string>();
    }
}
