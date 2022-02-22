// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Operations.DurableTask
{
    /// <summary>
    /// Represents the options for a "re-index" function.
    /// </summary>
    public class OrchestrationConcurrencyOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of concurrent orchestration instances.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int MaxInstances { get; set; }

        /// <summary>
        /// Gets or sets the name of the even that indicates the orchestration instance has completed.
        /// </summary>
        [Required]
        public string CompletionEvent { get; set; } = "Complete";

        /// <summary>
        /// Gets or sets the interval that orchestration instances are polled for their status.
        /// </summary>
        /// <remarks>
        /// The entire model cannot be push-based, as orchestration instances may fail unexpectedly.
        /// </remarks>
        [Range(typeof(TimeSpan), "00:01:00", "01:00:00", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true)]
        public TimeSpan PollingInterval { get; set; }
    }
}
