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
        private readonly VersionedCache<IVersioned> _versionedCache;

        public VersionedCacheTests()
        {
            _schemaVersionResolver = Substitute.For<ISchemaVersionResolver>();
            _versionedCache = new VersionedCache<IVersioned>(
                _schemaVersionResolver,
                new List<IVersioned> { new Example1(), new Example2() });
        }

        [Theory]
        [InlineData(SchemaVersion.Unknown)]
        [InlineData(SchemaVersion.V3)]
        [InlineData((SchemaVersion)2)]
        public async Task GivenInvalidVersion_WhenGettingValue_ThenThrowException(SchemaVersion version)
        {
            using CancellationTokenSource source = new CancellationTokenSource();

            _schemaVersionResolver.GetCurrentVersionAsync(source.Token).Returns(version);
            await Assert.ThrowsAsync<InvalidSchemaVersionException>(() => _versionedCache.GetAsync(source.Token));
            await _schemaVersionResolver.Received(1).GetCurrentVersionAsync(source.Token);
        }

        [Theory]
        [InlineData((SchemaVersion)SchemaVersionConstants.Min)]
        [InlineData((SchemaVersion)SchemaVersionConstants.Max)]
        [InlineData((SchemaVersion)SchemaVersionConstants.SupportDTAndTMInExtendedQueryTagSchemaVersion)]
        public async Task GivenValidVersion_WhenGettingValue_ThenReturnsValue(SchemaVersion version)
        {
            using CancellationTokenSource source = new CancellationTokenSource();

            _schemaVersionResolver.GetCurrentVersionAsync(source.Token).Returns(version);
            var value = await _versionedCache.GetAsync(source.Token);
            Assert.NotNull(value);
            await _schemaVersionResolver.Received(1).GetCurrentVersionAsync(source.Token);
        }

        [Fact]
        public async Task GivenValidVersion_WhenGettingValue_ThenReturnCachedValue()
        {
            using CancellationTokenSource source = new CancellationTokenSource();

            _schemaVersionResolver.GetCurrentVersionAsync(source.Token).Returns((SchemaVersion)SchemaVersionConstants.Min);
            Example1 first = (await _versionedCache.GetAsync(source.Token)) as Example1;
            Assert.NotNull(first);
            await _schemaVersionResolver.Received(1).GetCurrentVersionAsync(source.Token);

            _schemaVersionResolver.GetCurrentVersionAsync(source.Token).Returns((SchemaVersion)SchemaVersionConstants.Max);
            Example1 second = (await _versionedCache.GetAsync(source.Token)) as Example1;
            Assert.Same(first, second);
            await _schemaVersionResolver.Received(1).GetCurrentVersionAsync(source.Token);
        }

        private sealed class Example1 : IVersioned
        {
            public SchemaVersion Version => (SchemaVersion)SchemaVersionConstants.Min;
        }

        private sealed class Example2 : IVersioned
        {
            public SchemaVersion Version => (SchemaVersion)SchemaVersionConstants.Max;
        }
    }
}
