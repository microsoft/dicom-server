// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;

namespace Microsoft.Health.Dicom.Core.Features.Partitioning;

public class PartitionCache : EphemeralMemoryCache<string, Partition>
{
    public PartitionCache(IOptions<DataPartitionConfiguration> configuration, ILoggerFactory loggerFactory, ILogger<PartitionCache> logger)
        : base(configuration, loggerFactory, logger)
    {
    }
}
