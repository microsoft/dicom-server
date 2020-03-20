// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;
using Microsoft.Health.Fhir.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class SqlServerDicomIndexDataStoreTestHelper : IDicomIndexDataStoreTestHelper
    {
        private readonly string _connectionString;

        public SqlServerDicomIndexDataStoreTestHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IReadOnlyList<StudyMetadata>> GetStudyMetadataAsync(string studyInstanceUid)
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                var result = new List<StudyMetadata>();

                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = @$"
                        SELECT *
                        FROM {VLatest.StudyMetadataCore.TableName}
                        WHERE {VLatest.StudyMetadataCore.StudyInstanceUid} = @studyInstanceUid";

                    sqlCommand.Parameters.AddWithValue("@studyInstanceUid", studyInstanceUid);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            result.Add(new StudyMetadata(sqlDataReader));
                        }
                    }
                }

                return result;
            }
        }

        public async Task<IReadOnlyList<SeriesMetadata>> GetSeriesMetadataAsync(string seriesInstanceUid)
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                var result = new List<SeriesMetadata>();

                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = @$"
                        SELECT *
                        FROM {VLatest.SeriesMetadataCore.TableName}
                        WHERE {VLatest.SeriesMetadataCore.SeriesInstanceUid} = @seriesInstanceUid";

                    sqlCommand.Parameters.AddWithValue("@seriesInstanceUid", seriesInstanceUid);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            result.Add(new SeriesMetadata(sqlDataReader));
                        }
                    }
                }

                return result;
            }
        }

        public async Task<Instance> GetInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = @$"
                        SELECT *
                        FROM {VLatest.Instance.TableName}
                        WHERE {VLatest.Instance.StudyInstanceUid} = @studyInstanceUid
                            AND {VLatest.Instance.SeriesInstanceUid} = @seriesInstanceUid
                            AND {VLatest.Instance.SopInstanceUid} = @sopInstanceUid";

                    sqlCommand.Parameters.AddWithValue("@studyInstanceUid", studyInstanceUid);
                    sqlCommand.Parameters.AddWithValue("@seriesInstanceUid", seriesInstanceUid);
                    sqlCommand.Parameters.AddWithValue("@sopInstanceUid", sopInstanceUid);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        if (await sqlDataReader.ReadAsync())
                        {
                            return new Instance(sqlDataReader);
                        }

                        return null;
                    }
                }
            }
        }
    }
}
