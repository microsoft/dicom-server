// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class ExtendedQueryTagStoreTestHelper : IExtendedQueryTagStoreTestHelper
    {
        private readonly string _connectionString;
        public ExtendedQueryTagStoreTestHelper(string connectionString)
        {
            EnsureArg.IsNotNullOrWhiteSpace(connectionString);
            _connectionString = connectionString;
        }

        public async Task CleanupAsync(CancellationToken cancellationToken = default)
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    // Delete from ReindexState table
                    sqlCommand.CommandText = $"DELETE FROM {VLatest.ExtendedQueryTag.TableName}";
                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
