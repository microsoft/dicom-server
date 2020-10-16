// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Numerics;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.SqlServer.Features.Retrieve;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Storage;
using Microsoft.Health.Dicom.SqlServer.Features.Store;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Storage;
using NSubstitute;
using Polly;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class SqlDataStoreTestsFixture : IAsyncLifetime
    {
        private const string LocalConnectionString = "server=(local);Integrated Security=true";

        private readonly string _masterConnectionString;
        private readonly string _databaseName;
        private readonly SchemaInitializer _schemaInitializer;

        public SqlDataStoreTestsFixture()
        {
            string initialConnectionString = Environment.GetEnvironmentVariable("SqlServer:ConnectionString") ?? LocalConnectionString;

            _databaseName = $"DICOMINTEGRATIONTEST_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{BigInteger.Abs(new BigInteger(Guid.NewGuid().ToByteArray()))}";
            _masterConnectionString = new SqlConnectionStringBuilder(initialConnectionString) { InitialCatalog = "master" }.ToString();
            TestConnectionString = new SqlConnectionStringBuilder(initialConnectionString) { InitialCatalog = _databaseName }.ToString();

            var config = new SqlServerDataStoreConfiguration
            {
                ConnectionString = TestConnectionString,
                Initialize = true,
                SchemaOptions = new SqlServerSchemaOptions
                {
                    AutomaticUpdatesEnabled = true,
                },
            };

            var scriptProvider = new ScriptProvider<SchemaVersion>();

            var baseScriptProvider = new BaseScriptProvider();

            var mediator = Substitute.For<IMediator>();

            var sqlConnectionFactory = new DefaultSqlConnectionFactory(config);

            var schemaUpgradeRunner = new SchemaUpgradeRunner(scriptProvider, baseScriptProvider, mediator, NullLogger<SchemaUpgradeRunner>.Instance, sqlConnectionFactory);

            var schemaInformation = new SchemaInformation((int)SchemaVersion.V1, (int)SchemaVersion.V1);

            _schemaInitializer = new SchemaInitializer(config, schemaUpgradeRunner, schemaInformation, sqlConnectionFactory, NullLogger<SchemaInitializer>.Instance);

            var dicomSqlIndexSchema = new SqlIndexSchema(schemaInformation, NullLogger<SqlIndexSchema>.Instance);

            SqlTransactionHandler = new SqlTransactionHandler();

            SqlConnectionWrapperFactory = new SqlConnectionWrapperFactory(SqlTransactionHandler, new SqlCommandWrapperFactory(), sqlConnectionFactory);

            IndexDataStore = new SqlIndexDataStore(
                dicomSqlIndexSchema,
                SqlConnectionWrapperFactory);

            InstanceStore = new SqlInstanceStore(SqlConnectionWrapperFactory);

            TestHelper = new SqlIndexDataStoreTestHelper(TestConnectionString);
        }

        public SqlTransactionHandler SqlTransactionHandler { get; }

        public SqlConnectionWrapperFactory SqlConnectionWrapperFactory { get; }

        public string TestConnectionString { get; }

        public IIndexDataStore IndexDataStore { get; }

        public IInstanceStore InstanceStore { get; }

        public SqlIndexDataStoreTestHelper TestHelper { get; }

        public async Task InitializeAsync()
        {
            await Task.Run(() =>
            {
                // Change all asyn calls to sync to mitigate issue https://microsofthealth.visualstudio.com/Health/_workitems/edit/76630
                // Create the database
                using (var sqlConnection = new SqlConnection(_masterConnectionString))
                {
                    sqlConnection.Open();

                    using (SqlCommand command = sqlConnection.CreateCommand())
                    {
                        command.CommandTimeout = 600;
                        command.CommandText = $"CREATE DATABASE {_databaseName}";
                        command.ExecuteNonQuery();
                    }
                }

                // verify that we can connect to the new database. This sometimes does not work right away with Azure SQL.
                Policy
                    .Handle<SqlException>()
                    .WaitAndRetry(
                        retryCount: 7,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                    .Execute(() =>
                    {
                        using (var sqlConnection = new SqlConnection(TestConnectionString))
                        {
                            sqlConnection.Open();
                            using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                            {
                                sqlCommand.CommandText = "SELECT 1";
                                sqlCommand.ExecuteScalar();
                            }
                        }
                    });

                _schemaInitializer.Start();
            });
        }

        public async Task DisposeAsync()
        {
            await Task.Run(() =>
            {
                // Change all asyn calls to sync to mitigate issue https://microsofthealth.visualstudio.com/Health/_workitems/edit/76630
                using (var sqlConnection = new SqlConnection(_masterConnectionString))
                {
                    sqlConnection.Open();
                    SqlConnection.ClearAllPools();

                    using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                    {
                        sqlCommand.CommandTimeout = 600;
                        sqlCommand.CommandText = $"DROP DATABASE IF EXISTS {_databaseName}";
                        sqlCommand.ExecuteNonQuery();
                    }
                }
            });
        }
    }
}
