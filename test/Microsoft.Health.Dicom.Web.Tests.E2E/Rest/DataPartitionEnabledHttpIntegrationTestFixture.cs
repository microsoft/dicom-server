// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class DataPartitionEnabledHttpIntegrationTestFixture<TStartup> : HttpIntegrationTestFixture<TStartup>
    {
        public DataPartitionEnabledHttpIntegrationTestFixture()
            : this(Path.Combine("src"))
        {
        }

        protected DataPartitionEnabledHttpIntegrationTestFixture(string targetProjectParentDirectory)
            : base(targetProjectParentDirectory, enableDataPartitions: true)
        {
        }
    }
}
