// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data.SqlClient;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.Dicom.SqlServer
{
    public class ManagedIdentitySqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;

        public ManagedIdentitySqlConnectionFactory(SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration)
        {
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));

            _sqlServerDataStoreConfiguration = sqlServerDataStoreConfiguration;
        }

        /// <summary>
        /// Get sql connection after getting access token.
        /// </summary>
        /// <param name="connectToMaster">Should connect to master?</param>
        /// <returns>Sql connection task.</returns>
        public async Task<SqlConnection> GetSqlConnectionAsync(bool connectToMaster = false)
        {
            EnsureArg.IsNotNullOrEmpty(_sqlServerDataStoreConfiguration.ConnectionString);

            SqlConnectionStringBuilder connectionBuilder = connectToMaster ?
                new SqlConnectionStringBuilder(_sqlServerDataStoreConfiguration.ConnectionString) { InitialCatalog = string.Empty } :
                new SqlConnectionStringBuilder(_sqlServerDataStoreConfiguration.ConnectionString);

            SqlConnection sqlConnection = new SqlConnection(connectionBuilder.ToString());

            AzureServiceTokenProvider azureServiceTokenProvider = new Microsoft.Azure.Services.AppAuthentication.AzureServiceTokenProvider();
            var result = await azureServiceTokenProvider.GetAccessTokenAsync("https://database.windows.net/");
            sqlConnection.AccessToken = result;

            return sqlConnection;
        }

        /// <inheritdoc />
        public SqlConnection GetSqlConnection(bool connectToMaster = false)
        {
            return GetSqlConnectionAsync(connectToMaster).Result;
        }
    }
}
