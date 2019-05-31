// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Blob.Configs
{
    public class BlobDataStoreRequestOptions
    {
        public int ExponentialRetryBackoffDeltaInSeconds { get; set; } = 4;

        public int ExponentialRetryMaxAttempts { get; set; } = 6;

        public int ServerTimeoutInMinutes { get; set; } = 2;

        public int ParallelOperationThreadCount { get; set; } = 2;
    }
}
