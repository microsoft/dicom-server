// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;

public class FramesRangeCache : EphemeralMemoryCache<long, IReadOnlyDictionary<int, FrameRange>>, IFramesRangeCache
{
    public FramesRangeCache(IOptions<FramesRangeCacheConfiguration> configuration, ILoggerFactory loggerFactory, ILogger<FramesRangeCache> logger)
        : base(configuration, loggerFactory, logger)
    {
    }
}

