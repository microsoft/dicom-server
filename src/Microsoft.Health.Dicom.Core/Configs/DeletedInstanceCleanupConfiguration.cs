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
        /// The amount of time to wait before the initial attempt to cleanup an instance
        /// </summary>
        public TimeSpan DeleteDelay { get; set; }

        /// <summary>
        /// The maximum number of times to attempt to cleanup a deleted entry
        /// </summary>
        public int MaxRetries { get; set; }

        /// <summary>
        /// The amount of time to back off between retries
        /// </summary>
        public TimeSpan RetryBackOff { get; set; }

        /// <summary>
        /// The amount of time to wait between polling for new entries to cleanup
        /// </summary>
        public TimeSpan PollingInterval { get; set; }

        /// <summary>
        /// The number of items to grab per batch
        /// </summary>
        public int BatchSize { get; set; }
    }
}
