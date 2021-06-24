// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.SqlServer.Features.Common;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
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
        [InlineData(null, SchemaVersion.Unknown)]
        [InlineData(1, SchemaVersion.V1)]
        [InlineData(2, SchemaVersion.V2)]
        [InlineData(123, (SchemaVersion)123)]
        public async Task GivenSuccessfulQuery_WhenGettingCurrentVersion_ThenReturnCurrentVersion(int? version, SchemaVersion expected)
        {
            IDbConnectionFactory dbConnectionFactory = Substitute.For<IDbConnectionFactory>();
            var resolver = new SqlSchemaVersionResolver(dbConnectionFactory);

            using var tokenSource = new CancellationTokenSource();
            await using (SqlCommandValidation.ForVersion(dbConnectionFactory, tokenSource.Token, version))
            {
                Assert.Equal(expected, await resolver.GetCurrentVersionAsync(tokenSource.Token));
            }
        }

        [Fact]
        public async Task GivenDBNull_WhenGettingCurrentVersion_ThenReturnUnknownVersion()
        {
            IDbConnectionFactory dbConnectionFactory = Substitute.For<IDbConnectionFactory>();
            var resolver = new SqlSchemaVersionResolver(dbConnectionFactory);

            using var tokenSource = new CancellationTokenSource();
            await using (SqlCommandValidation.ForDBNull(dbConnectionFactory, tokenSource.Token))
            {
                Assert.Equal(SchemaVersion.Unknown, await resolver.GetCurrentVersionAsync(tokenSource.Token));
            }
        }

        [Fact]
        public async Task GivenUnknownError_WhenGettingCurrentVersion_ThenThrowException()
        {
            IDbConnectionFactory dbConnectionFactory = Substitute.For<IDbConnectionFactory>();
            var resolver = new SqlSchemaVersionResolver(dbConnectionFactory);

            using var tokenSource = new CancellationTokenSource();
            await using (SqlCommandValidation.ForException(dbConnectionFactory, tokenSource.Token, new IOException()))
            {
                await Assert.ThrowsAsync<IOException>(() => resolver.GetCurrentVersionAsync(tokenSource.Token));
            }
        }

        private sealed class SqlCommandValidation : IAsyncDisposable
        {
            private readonly IDbConnectionFactory _dbConnectionFactory;
            private readonly DbConnection _dbConnection;
            private readonly DbCommand _dbCommand;
            private readonly CancellationToken _cancellationToken;

            private SqlCommandValidation(
                IDbConnectionFactory dbConnectionFactory,
                CancellationToken cancellationToken,
                object response = null,
                Exception exception = null)
            {
                _dbConnectionFactory = EnsureArg.IsNotNull(dbConnectionFactory, nameof(dbConnectionFactory));
                _dbConnection = Substitute.For<DbConnection>();
                _dbCommand = Substitute.For<DbCommand>();
                _cancellationToken = cancellationToken;

                _dbConnectionFactory.GetConnectionAsync(cancellationToken).Returns(_dbConnection);
                _dbConnection.CreateCommand().Returns(_dbCommand);

                if (exception == null)
                {
                    _dbCommand.ExecuteScalarAsync(cancellationToken).Returns(Task.FromResult(response));
                }
                else
                {
                    _dbCommand.ExecuteScalarAsync(cancellationToken).Returns(Task.FromException<object>(exception));
                }
            }

            public async ValueTask DisposeAsync()
            {
                Assert.Equal(CommandType.StoredProcedure, _dbCommand.CommandType);
                Assert.Equal(SqlSchemaVersionResolver.VersionStoredProcedure, _dbCommand.CommandText);

                await _dbConnectionFactory.Received(1).GetConnectionAsync(_cancellationToken);
                await _dbConnection.Received(1).OpenAsync(_cancellationToken);
                _dbConnection.Received(1).CreateCommand();
                await _dbCommand.Received(1).ExecuteScalarAsync(_cancellationToken);

                _dbCommand.Dispose();
                _dbConnection.Dispose();
            }

            public static SqlCommandValidation ForVersion(
                IDbConnectionFactory dbConnectionFactory,
                CancellationToken cancellationToken,
                int? response)
            {
                return new SqlCommandValidation(dbConnectionFactory, cancellationToken, response: response);
            }

            public static SqlCommandValidation ForDBNull(
                IDbConnectionFactory dbConnectionFactory,
                CancellationToken cancellationToken)
            {
                return new SqlCommandValidation(dbConnectionFactory, cancellationToken, response: DBNull.Value);
            }

            public static SqlCommandValidation ForException(
                IDbConnectionFactory dbConnectionFactory,
                CancellationToken cancellationToken,
                Exception exception)
            {
                return new SqlCommandValidation(dbConnectionFactory, cancellationToken, exception: exception);
            }
        }
    }
}
