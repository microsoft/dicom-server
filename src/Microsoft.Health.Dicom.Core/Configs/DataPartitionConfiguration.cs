// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Configs
{
    /// <summary>
    /// Configuration for data partition feature.
    /// </summary>
    public class DataPartitionConfiguration
    {
        /// <summary>
        /// Maximum number of data partitions to cache - around 1.4Mb max size when estimating 140 bytes per entry.
        /// </summary>
        public int MaxCacheSize { get; set; } = 10000;

        /// <summary>
        /// Maximum cache expiration time for an entry in minutes
        /// </summary>
        public int MaxCacheAbsoluteExpirationInMinutes { get; set; } = 5;
    }
}
