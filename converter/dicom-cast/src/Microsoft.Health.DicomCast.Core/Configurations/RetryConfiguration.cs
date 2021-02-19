// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.DicomCast.Core.Configurations
{
    /// <summary>
    /// The configuration for retryable exceptions.
    /// </summary>
    public class RetryConfiguration
    {
        /// <summary>
        /// The total amount of time to spend retrying a single change feed entry across all retries.
        /// </summary>
        public TimeSpan TotalRetryDuration { get; set; } = TimeSpan.FromMinutes(10);
    }
}
