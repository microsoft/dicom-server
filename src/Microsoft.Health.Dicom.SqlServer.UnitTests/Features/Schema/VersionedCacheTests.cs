// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.SqlServer.Exceptions;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.SqlServer.UnitTests.Features.Schema
{
    public class VersionedCacheTests
    {
        private readonly ISchemaVersionResolver _schemaVersionResolver;
        private readonly VersionedCache<SqlStore> _versionedCache;

        public VersionedCacheTests()
        {
            _schemaVersionResolver = Substitute.For<ISchemaVersionResolver>();
            _versionedCache = new VersionedCache<SqlStore>(
                _schemaVersionResolver,
                new List<SqlStore>
                {
                    new SqlStore { Version = SchemaVersion.V2 },
                    new SqlStore { Version = SchemaVersion.V4 },
                    new SqlStore { Version = SchemaVersion.V7 },
                });
        }

        [Theory]
        [InlineData(SchemaVersion.Unknown)]
        [InlineData(SchemaVersion.V1)]
        public async Task GivenInvalidVersion_WhenGettingValue_ThenThrowException(SchemaVersion version)
        {
            using CancellationTokenSource source = new CancellationTokenSource();

            _schemaVersionResolver.GetCurrentVersionAsync(source.Token).Returns(version);
            await Assert.ThrowsAsync<InvalidSchemaVersionException>(() => _versionedCache.GetAsync(source.Token));
            await _schemaVersionResolver.Received(1).GetCurrentVersionAsync(source.Token);
        }

        [Theory]
        [InlineData(SchemaVersion.V2, SchemaVersion.V2)]
        [InlineData(SchemaVersion.V3, SchemaVersion.V2)]
        [InlineData(SchemaVersion.V4, SchemaVersion.V4)]
        [InlineData(SchemaVersion.V5, SchemaVersion.V4)]
        [InlineData(SchemaVersion.V6, SchemaVersion.V4)]
        [InlineData(SchemaVersion.V7, SchemaVersion.V7)]
        [InlineData(SchemaVersion.V8, SchemaVersion.V7)]
        [InlineData(SchemaVersion.V9, SchemaVersion.V7)]
        public async Task GivenValidVersion_WhenGettingValue_ThenReturnsValue(SchemaVersion current, SchemaVersion expected)
        {
            SqlStore actual;
            using CancellationTokenSource source = new CancellationTokenSource();

            // Resolve version
            _schemaVersionResolver.GetCurrentVersionAsync(source.Token).Returns(current);
            actual = await _versionedCache.GetAsync(source.Token);
            Assert.Equal(expected, actual.Version);
            await _schemaVersionResolver.Received(1).GetCurrentVersionAsync(source.Token);

            // Use cached current version
            _schemaVersionResolver.GetCurrentVersionAsync(source.Token).Returns((SchemaVersion)SchemaVersionConstants.Max);
            actual = await _versionedCache.GetAsync(source.Token);
            Assert.Equal(expected, actual.Version);
            await _schemaVersionResolver.Received(1).GetCurrentVersionAsync(source.Token);
        }

        private sealed class SqlStore : IVersioned
        {
            public SchemaVersion Version { get; init; }
        }
    }
}
