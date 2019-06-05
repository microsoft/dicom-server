// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Health;
using Microsoft.Health.Blob.Features.Storage;

namespace Microsoft.Health.Blob.UnitTests.Features.Health
{
    internal class TestBlobHealthCheck : BlobHealthCheck
    {
        public const string TestBlobHealthCheckName = "TestBlobHealthCheck";

        public TestBlobHealthCheck(
            CloudBlobClient client,
            BlobDataStoreConfiguration configuration,
            IOptionsSnapshot<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
            IBlobClientTestProvider testProvider,
            ILogger<TestBlobHealthCheck> logger)
            : base(
                  client,
                  configuration,
                  namedBlobContainerConfigurationAccessor,
                  TestBlobHealthCheckName,
                  testProvider,
                  logger)
        {
        }
    }
}
