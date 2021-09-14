// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using StoredProcedure = Microsoft.SqlServer.Management.Smo.StoredProcedure;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    internal static class SqlTestUtils
    {
        /// <summary>
        /// Clear table content asynchronously.
        /// </summary>
        /// <param name="connectionString">Database connection string.</param>
        /// <param name="tableName">Name of table to be cleared.</param>
        /// <returns>The task.</returns>
        public static async Task ClearTableAsync(string connectionString, string tableName)
        {
            EnsureArg.IsNotNullOrWhiteSpace(connectionString, nameof(connectionString));
            EnsureArg.IsNotNullOrWhiteSpace(tableName, nameof(tableName));
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync();
                SqlConnection.ClearAllPools();
                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = @$"
                        DELETE
                        FROM {tableName}";

                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }

        public static async Task<System.Collections.Generic.IReadOnlyList<StoredProcedure>> GetStoredProceduresAsync(SqlDataStoreTestsFixture sqlStore, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(sqlStore, nameof(sqlStore));
            using var connectionWraper = await sqlStore.SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
            ServerConnection connection = new ServerConnection(connectionWraper.SqlConnection);
            Server server = new Server(connection);
            Database db = server.Databases[sqlStore.DatabaseName];
            List<StoredProcedure> result = new List<StoredProcedure>();
            DataTable dataTable = db.EnumObjects(DatabaseObjectTypes.StoredProcedure);
            foreach (DataRow row in dataTable.Rows)
            {
                string schema = (string)row["Schema"];
                if (schema == "sys" || schema == "INFORMATION_SCHEMA")
                {
                    continue;
                }
                StoredProcedure sp = (StoredProcedure)server.GetSmoObject(new Urn((string)row["Urn"]));
                if (!sp.IsSystemObject)
                {
                    result.Add(sp);
                }
            }
            return result;
        }
    }
}
