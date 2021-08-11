// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;

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
    }
}
