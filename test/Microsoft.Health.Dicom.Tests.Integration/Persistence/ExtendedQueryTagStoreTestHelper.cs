// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class ExtendedQueryTagStoreTestHelper : IExtendedQueryTagStoreTestHelper
    {
        private readonly string _connectionString;

        private static readonly IReadOnlyDictionary<ExtendedQueryTagDataType, string> DateTypeAndTableNameMapping = new Dictionary<ExtendedQueryTagDataType, string>()
            {
                { ExtendedQueryTagDataType.StringData, VLatest.ExtendedQueryTagString.TableName },
                { ExtendedQueryTagDataType.LongData, VLatest.ExtendedQueryTagLong.TableName },
                { ExtendedQueryTagDataType.DoubleData, VLatest.ExtendedQueryTagDouble.TableName },
                { ExtendedQueryTagDataType.DateTimeData, VLatest.ExtendedQueryTagDateTime.TableName },
                { ExtendedQueryTagDataType.PersonNameData, VLatest.ExtendedQueryTagPersonName.TableName },
            };

        public ExtendedQueryTagStoreTestHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        async Task<IReadOnlyList<ExtendedQueryTagDataRow>> IExtendedQueryTagStoreTestHelper.GetExtendedQueryTagDataAsync(
               ExtendedQueryTagDataType dataType,
               int tagKey,
               long studyKey,
               long? seriesKey,
               long? instanceKey,
               CancellationToken cancellationToken)
        {
            var results = new List<ExtendedQueryTagDataRow>();
            string tagKeyParam = "@tagKey";
            string studyKeyParam = "@studyKey";
            string seriesKeyParam = "@seriesKey";
            string instanceKeyParam = "@instanceKey";

            // Columns on all extended query tag index data tables are of same names
            string studyKeyColName = VLatest.ExtendedQueryTagString.StudyKey.Metadata.Name;
            string seriesKeyColName = VLatest.ExtendedQueryTagString.SeriesKey.Metadata.Name;
            string instanceKeyColName = VLatest.ExtendedQueryTagString.InstanceKey.Metadata.Name;
            string tagKeyName = VLatest.ExtendedQueryTagString.TagKey.Metadata.Name;
            string seriesFilter = seriesKey.HasValue ? $"{seriesKeyColName} = {seriesKeyParam}" : $"{seriesKeyColName} IS NULL";
            string instanceFilter = instanceKey.HasValue ? $"{instanceKeyColName} = {instanceKeyParam}" : $"{instanceKeyColName} IS NULL";

            return await GetExtendedQueryTagRowsAsync(
                dataType,
                sqlCommand =>
                {
                    sqlCommand.CommandText = @$"
                        SELECT *
                        FROM {DateTypeAndTableNameMapping[dataType]}
                        WHERE 
                            {tagKeyName} = {tagKeyParam}
                            AND {studyKeyColName} = {studyKeyParam}
                            AND {seriesFilter}
                            AND {instanceFilter}
                    ";

                    sqlCommand.Parameters.AddWithValue(tagKeyParam, tagKey);
                    sqlCommand.Parameters.AddWithValue(studyKeyParam, studyKey);
                    sqlCommand.Parameters.AddWithValue(seriesKeyParam, seriesKey.HasValue ? seriesKey.Value : DBNull.Value);
                    sqlCommand.Parameters.AddWithValue(instanceKeyParam, instanceKey.HasValue ? instanceKey.Value : DBNull.Value);
                },
                cancellationToken);
        }

        async Task<IReadOnlyList<ExtendedQueryTagDataRow>> IExtendedQueryTagStoreTestHelper.GetExtendedQueryTagDataForTagKeyAsync(ExtendedQueryTagDataType dataType, int tagKey, CancellationToken cancellationToken)
        {
            string tagKeyParam = "@tagKey";

            return await GetExtendedQueryTagRowsAsync(
                dataType,
                sqlCommand =>
                {
                    sqlCommand.CommandText = @$"
                            SELECT *
                            FROM {DateTypeAndTableNameMapping[dataType]}
                            WHERE 
                                {VLatest.ExtendedQueryTagString.TagKey} = {tagKeyParam}
                            
                        ";

                    sqlCommand.Parameters.AddWithValue(tagKeyParam, tagKey);
                },
                cancellationToken);
        }

        private async Task<IReadOnlyList<ExtendedQueryTagDataRow>> GetExtendedQueryTagRowsAsync(ExtendedQueryTagDataType dataType, Action<SqlCommand> filler, CancellationToken cancellationToken)
        {
            var results = new List<ExtendedQueryTagDataRow>();
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync(cancellationToken);

                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    filler(sqlCommand);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync(cancellationToken))
                    {
                        if (await sqlDataReader.ReadAsync(cancellationToken))
                        {
                            ExtendedQueryTagDataRow row = new ExtendedQueryTagDataRow();
                            row.Read(sqlDataReader, dataType);
                            results.Add(row);
                        }
                    }
                }
            }

            return results;
        }

        public async Task ClearExtendedQueryTagTables()
        {
            await ClearTable(VLatest.ExtendedQueryTag.TableName);
            await ClearTable(VLatest.ExtendedQueryTagString.TableName);
            await ClearTable(VLatest.ExtendedQueryTagDouble.TableName);
            await ClearTable(VLatest.ExtendedQueryTagPersonName.TableName);
            await ClearTable(VLatest.ExtendedQueryTagLong.TableName);
            await ClearTable(VLatest.ExtendedQueryTagDateTime.TableName);
        }

        private async Task ClearTable(string tableName)
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

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
