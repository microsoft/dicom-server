// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.SqlServer.Exceptions;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.SqlServer.UnitTests.Features.Schema;

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

    [Fact]
    public async Task GivenInvalidVersion_WhenGettingValue_ThenThrowException()
    {
        using var source = new CancellationTokenSource();

        _schemaVersionResolver.GetCurrentVersionAsync(source.Token).Returns(SchemaVersion.V1);
        await Assert.ThrowsAsync<InvalidSchemaVersionException>(() => _versionedCache.GetAsync(source.Token));
        await _schemaVersionResolver.Received(1).GetCurrentVersionAsync(source.Token);
    }

    [Fact]
    public async Task GivenAnUnknownVersion_WhenGettingValue_ThenThrowException()
    {
        using var source = new CancellationTokenSource();

        _schemaVersionResolver.GetCurrentVersionAsync(source.Token).Returns(SchemaVersion.Unknown);
        await Assert.ThrowsAsync<DataStoreNotReadyException>(() => _versionedCache.GetAsync(source.Token));
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
        using var source = new CancellationTokenSource();

        // Resolve version
        _schemaVersionResolver.GetCurrentVersionAsync(source.Token).Returns(current);
        SqlStore actual = await _versionedCache.GetAsync(source.Token);
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
