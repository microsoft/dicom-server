// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.SqlServer.UnitTests.Features.Schema
{
    public class SqlSchemaVersionResolverTests
    {
        [Fact]
        public void GivenNullArgument_WhenConstructing_ThenThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SqlSchemaVersionResolver(null));
        }

        [Theory]
        [InlineData(-12, (SchemaVersion)(-12))]
        [InlineData(1, SchemaVersion.V1)]
        [InlineData(4, SchemaVersion.V4)]
        [InlineData(100, (SchemaVersion)100)]
        public async Task GivenVersion_WhenGettingCurrentVersion_ThenConvertToSchemaVersion(int version, SchemaVersion expected)
        {
            IReadOnlySchemaManagerDataStore schemaManager = Substitute.For<IReadOnlySchemaManagerDataStore>();
            var resolver = new SqlSchemaVersionResolver(schemaManager);

            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            schemaManager.GetCurrentSchemaVersionAsync(tokenSource.Token).Returns(Task.FromResult(version));

            Assert.Equal(expected, await resolver.GetCurrentVersionAsync(tokenSource.Token));
        }
    }
}
