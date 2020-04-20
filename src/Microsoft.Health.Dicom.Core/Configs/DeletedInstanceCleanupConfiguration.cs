// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Configs
{
    public class DeletedInstanceCleanupConfiguration
    {
        /// <summary>
        /// The number of seconds to wait before the initial attempt to cleanup an instance
        /// </summary>
        public int DeleteDelay { get; set; }

        /// <summary>
        /// The maximum number of times to attempt to cleanup a deleted entry
        /// </summary>
        public int MaxRetries { get; set; }

        /// <summary>
        /// The amount of time to back off between retries in seconds
        /// </summary>
        public int RetryBackOff { get; set; }

        /// <summary>
        /// The amount of time to wait between polling for new entries to cleanup in seconds
        /// </summary>
        public int PollingInterval { get; set; }

        /// <summary>
        /// The number of items to grab per batch
        /// </summary>
        public int BatchSize { get; set; }
    }
}
