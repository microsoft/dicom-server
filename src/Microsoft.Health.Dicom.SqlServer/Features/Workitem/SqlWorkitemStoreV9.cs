// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Workitem
{
    internal class SqlWorkitemStoreV9 : ISqlWorkitemStore
    {
        protected SqlConnectionWrapperFactory SqlConnectionWrapperFactory;

        public SqlWorkitemStoreV9(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        {
            SqlConnectionWrapperFactory = EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
        }

        public virtual SchemaVersion Version => SchemaVersion.V9;

        public virtual async Task<long> BeginAddWorkitemAsync(int partitionKey, DicomDataset dataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                var rows = ExtendedQueryTagDataRowsBuilder.Build(dataset, queryTags, Version);
                var parameters = new VLatest.AddWorkitemTableValuedParameters(
                    rows.StringRows,
                    rows.DateTimeWithUtcRows,
                    rows.PersonNameRows
                );

                string workitemUid = dataset.GetString(DicomTag.SOPInstanceUID);

                VLatest.AddWorkitem.PopulateCommand(
                    sqlCommandWrapper,
                    partitionKey,
                    workitemUid,
                    (byte)IndexStatus.Creating,
                    parameters);

                try
                {
                    return (long)await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    if (ex.Number == SqlErrorCodes.Conflict)
                    {
                        throw new WorkitemAlreadyExistsException(workitemUid);
                    }

                    throw new DataStoreException(ex);
                }
            }
        }

        public async Task<long> UpdateWorkitemAsync(int partitionKey, string workitemUid, DicomDataset dataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                var rows = ExtendedQueryTagDataRowsBuilder.Build(dataset, queryTags, Version);
                var parameters = new VLatest.UpdateWorkitemTableValuedParameters(
                    rows.StringRows,
                    rows.DateTimeWithUtcRows,
                    rows.PersonNameRows
                );

                VLatest.UpdateWorkitem.PopulateCommand(
                    sqlCommandWrapper,
                    partitionKey,
                    workitemUid,
                    parameters);

                try
                {
                    return (long)await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    throw new DataStoreException(ex);
                }
            }
        }

        public async Task DeleteWorkitemAsync(int partitionKey, string workitemUid, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.DeleteWorkitem.PopulateCommand(
                    sqlCommandWrapper,
                    partitionKey,
                    workitemUid); ;

                try
                {
                    await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    throw new DataStoreException(ex);
                }
            }
        }

        public virtual async Task EndAddWorkitemAsync(int partitionKey, long workitemKey, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.UpdateWorkitemStatus.PopulateCommand(
                    sqlCommandWrapper,
                    partitionKey,
                    workitemKey,
                    (byte)IndexStatus.Created); ;

                try
                {
                    await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    throw new DataStoreException(ex);
                }
            }
        }

        public virtual async Task<IReadOnlyList<WorkitemQueryTagStoreEntry>> GetWorkitemQueryTagsAsync(CancellationToken cancellationToken = default)
        {
            var results = new List<WorkitemQueryTagStoreEntry>();

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.GetWorkitemQueryTags.PopulateCommand(sqlCommandWrapper);

                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (int tagKey, string tagPath, string tagVR) = reader.ReadRow(
                            VLatest.ExtendedQueryTag.TagKey,
                            VLatest.ExtendedQueryTag.TagPath,
                            VLatest.ExtendedQueryTag.TagVR);

                        results.Add(new WorkitemQueryTagStoreEntry(tagKey, tagPath, tagVR));
                    }
                }
            }

            return results;
        }

        public async Task<WorkitemDetail> GetWorkitemDetailAsync(int partitionKey, string workitemUid, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.GetWorkitemDetail.PopulateCommand(sqlCommandWrapper, partitionKey, workitemUid);

                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (string wiUid, long workitemKey, int pkey, string procedureStepState) = reader.ReadRow(
                            new Health.SqlServer.Features.Schema.Model.VarCharColumn("WorkitemUid", 64),
                            new Health.SqlServer.Features.Schema.Model.BigIntColumn("WorkitemKey"),
                            new Health.SqlServer.Features.Schema.Model.IntColumn("PartitionKey"),
                            new Health.SqlServer.Features.Schema.Model.NVarCharColumn("ProcedureStepState", 64));

                        return new WorkitemDetail
                        {
                            WorkitemKey = workitemKey,
                            WorkitemUid = wiUid,
                            PartitionKey = pkey,
                            ProcedureStepState = procedureStepState
                        };
                    }
                }
            }

            return null;
        }
    }
}
