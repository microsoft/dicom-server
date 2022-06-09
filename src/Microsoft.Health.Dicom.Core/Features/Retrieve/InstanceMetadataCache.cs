// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;

public class InstanceMetadataCache : EphemeralMemoryCache<InstanceIdentifier, InstanceMetadata>
{
    public InstanceMetadataCache(IOptions<InstanceMetadataCacheConfiguration> configuration, ILoggerFactory loggerFactory, ILogger<EphemeralMemoryCache<InstanceIdentifier, InstanceMetadata>> logger)
        : base(configuration, loggerFactory, logger)
    {
    }
}
