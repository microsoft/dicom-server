// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.RateLimiting;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;
public interface IRateLimiterFactory
{
    RateLimiter GetRateLimiter();
}
