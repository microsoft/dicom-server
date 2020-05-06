// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class SqlChangeFeedStoreTestHelper : ISqlChangeFeedStoreTestHelper
    {
        private readonly string _connectionString;

        public SqlChangeFeedStoreTestHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IReadOnlyList<ChangeFeedRow>> GetChangeFeedRowsAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                var result = new List<ChangeFeedRow>();

                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = @$"
                        SELECT *
                        FROM {VLatest.ChangeFeed.TableName}
                        WHERE {VLatest.ChangeFeed.StudyInstanceUid} = @studyInstanceUid
                        AND {VLatest.ChangeFeed.SeriesInstanceUid} = @seriesInstanceUid
                        AND {VLatest.ChangeFeed.SopInstanceUid} = @sopInstanceUid
                        ORDER BY {VLatest.ChangeFeed.Sequence}";

                    sqlCommand.Parameters.AddWithValue("@studyInstanceUid", studyInstanceUid);
                    sqlCommand.Parameters.AddWithValue("@seriesInstanceUid", seriesInstanceUid);
                    sqlCommand.Parameters.AddWithValue("@sopInstanceUid", sopInstanceUid);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            result.Add(new ChangeFeedRow(sqlDataReader));
                        }
                    }
                }

                return result;
            }
        }
    }
}
