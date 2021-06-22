// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.Management
{
    public class OrchestrationsHistoryConfiguration
    {
        public const string SectionName = "OrchestrationsHistory";

        [Required]
        [MinLength(1)]
        public IReadOnlyCollection<OrchestrationRuntimeStatus> RuntimeStatuses { get; set; }

        [Range(0, 1000)]
        public int MinimumAgeDays { get; set; } = 30;

        [Required]
        public string PurgeFrequency { get; set; }

        public const string PurgeFrequencyVariable =
            "%" + Startup.HostSectionName + ":" + SectionName + ":" + nameof(PurgeFrequency) + "%";
    }
}
