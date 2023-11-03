// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.RateLimiting;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;
internal class RetrieveRateLimiter : IRateLimiterFactory
{
    private readonly RateLimiter _rateLimiter;

    public RetrieveRateLimiter(RateLimiter limiter)
    {
        _rateLimiter = limiter;
    }

    public RateLimiter GetRateLimiter() => _rateLimiter;
}
