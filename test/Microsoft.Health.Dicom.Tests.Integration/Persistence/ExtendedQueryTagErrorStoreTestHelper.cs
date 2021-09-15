// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class ExtendedQueryTagErrorStoreTestHelper : IExtendedQueryTagErrorStoreTestHelper
    {
        private readonly string _connectionString;

        public ExtendedQueryTagErrorStoreTestHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Task ClearExtendedQueryTagErrorTableAsync()
        {
            return SqlTestUtils.ClearTableAsync(_connectionString, VLatest.ExtendedQueryTagError.TableName);
        }

        public async Task<bool> DoesExtendedQueryTagErrorExistAsync(int tagKey)
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = @$"
                        SELECT *
                        FROM {VLatest.ExtendedQueryTagError.TableName}
                        WHERE {VLatest.ExtendedQueryTagError.TagKey} = @tagKey";

                    sqlCommand.Parameters.AddWithValue("@tagKey", tagKey);

                    SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync();
                    return sqlDataReader.HasRows;
                }
            }
        }
    }
}
