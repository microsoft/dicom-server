// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Configs;

/// <summary>
/// Configuration for data partition feature.
/// </summary>
public class DataPartitionConfiguration : CacheConfiguration
{
    public DataPartitionConfiguration()
    {
        MaxCacheSize = 10000;
        MaxCacheAbsoluteExpirationInMinutes = 5;
    }
}
