// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.Management
{
    public class PurgeHistoryOptions
    {
        internal const string SectionName = "PurgeHistory";

        [Required]
        [MinLength(1)]
        public IReadOnlyCollection<OrchestrationRuntimeStatus> RuntimeStatuses { get; set; }

        [Range(0, 365)]
        public int MinimumAgeDays { get; set; } = 30;

        [Required]
        public string Frequency { get; set; }
    }
}
