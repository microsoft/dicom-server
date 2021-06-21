// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema;
using Xunit;

namespace Microsoft.Health.Dicom.SqlServer.UnitTests.Features.Schema
{
    public class BackgroundSchemaVersionResolverTests
    {
        [Fact]
        public void GivenNullArgument_WhenConstructing_ThenThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new BackgroundSchemaVersionResolver(null));
        }

        [Fact]
        public async Task GivenAnyInvocation_WhenGettingCurrentVersion_ThenReturnCurrentVersion()
        {
            var resolver = new BackgroundSchemaVersionResolver(new SchemaInformation(1, 3) { Current = 2 });
            Assert.Equal(SchemaVersion.V2, await resolver.GetCurrentVersionAsync(default));
        }
    }
}
