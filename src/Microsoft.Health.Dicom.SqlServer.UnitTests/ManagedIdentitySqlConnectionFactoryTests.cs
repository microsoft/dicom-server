// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data.SqlClient;
using Microsoft.Health.SqlServer.Configs;
using Xunit;

namespace Microsoft.Health.Dicom.SqlServer.UnitTests
{
    public class ManagedIdentitySqlConnectionFactoryTests
    {
        private const string DatabaseName = "Dicom";
        private const string ServerName = "tcp:alimanagedidentitytest.database.windows.net,1433";

        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
        private readonly ManagedIdentitySqlConnectionFactory _sqlConnectionFactory;

        public ManagedIdentitySqlConnectionFactoryTests()
        {
            _sqlServerDataStoreConfiguration = new SqlServerDataStoreConfiguration();
            _sqlServerDataStoreConfiguration.ConnectionString = $"Server={ServerName};Database={DatabaseName};";
            _sqlServerDataStoreConfiguration.ConnectionType = SqlServerConnectionType.ManagedIdentity;
            _sqlConnectionFactory = new ManagedIdentitySqlConnectionFactory(_sqlServerDataStoreConfiguration);
        }

        [Fact]
        public void GivenManagedIdentityConnectionType_WhenSqlConnectionRequested_AccessTokenIsSet()
        {
            SqlConnection sqlConnection = _sqlConnectionFactory.GetSqlConnection();

            Assert.NotNull(sqlConnection.AccessToken);
        }

        [Fact]
        public void GivenDefaultConnectionType_WhenSqlConnectionRequested_DatabaseIsSet()
        {
            SqlConnection sqlConnection = _sqlConnectionFactory.GetSqlConnection();

            Assert.Equal(DatabaseName, sqlConnection.Database);
        }

        [Fact]
        public void GivenDefaultConnectionType_WhenSqlConnectionToMasterRequested_MasterDatabaseIsSet()
        {
            SqlConnection sqlConnection = _sqlConnectionFactory.GetSqlConnection(connectToMaster: true);

            Assert.Empty(sqlConnection.Database);
        }
    }
}
