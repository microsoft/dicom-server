// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Configs
{
    public class DeletedInstanceCleanupConfiguration
    {
        /// <summary>
        /// The amount of time to wait before the initial attempt to cleanup an instance.
        /// Default: 3 days
        /// </summary>
        public TimeSpan DeleteDelay { get; set; } = TimeSpan.FromDays(3);

        /// <summary>
        /// The maximum number of times to attempt to cleanup a deleted entry.
        /// Default: 5
        /// </summary>
        public int MaxRetries { get; set; } = 5;

        /// <summary>
        /// The amount of time to back off between cleanup retries.
        /// Default: 1 day
        /// </summary>
        public TimeSpan RetryBackOff { get; set; } = TimeSpan.FromDays(1);

        /// <summary>
        /// The amount of time to wait between polling for new entries to cleanup.
        /// Default: 3 minutes
        /// </summary>
        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMinutes(3);

        /// <summary>
        /// The number of items to grab per batch.
        /// Default: 10
        /// </summary>
        public int BatchSize { get; set; } = 10;
    }
}
