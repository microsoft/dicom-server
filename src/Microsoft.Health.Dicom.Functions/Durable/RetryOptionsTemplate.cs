// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Functions.Durable
{
    internal sealed class RetryOptionsTemplate
    {
        public TimeSpan FirstRetryInterval { get; set; } = TimeSpan.Zero;

        public TimeSpan MaxRetryInterval { get; set; } = TimeSpan.FromDays(6.0); // Durable Function Maximum

        public double BackoffCoefficient { get; set; } = 1;

        public TimeSpan RetryTimeout { get; set; } = TimeSpan.MaxValue;

        public int MaxNumberOfAttempts { get; set; } = 1;
    }
}
