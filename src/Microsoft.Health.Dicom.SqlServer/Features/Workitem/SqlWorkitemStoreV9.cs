// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Extensions;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Workitem
{
    internal class SqlWorkitemStoreV9 : ISqlWorkitemStore
    {
        protected SqlConnectionWrapperFactory SqlConnectionWrapperFactory;
        protected readonly ILogger<ISqlWorkitemStore> Logger;

        public SqlWorkitemStoreV9(SqlConnectionWrapperFactory sqlConnectionWrapperFactory, ILogger<ISqlWorkitemStore> logger)
        {
            SqlConnectionWrapperFactory = EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            Logger = EnsureArg.IsNotNull(logger, nameof(logger));
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

        public virtual async Task EndAddWorkitemAsync(int partitionKey, long workitemKey, CancellationToken cancellationToken = default)
        {
            await UpdateWorkitemStatusAsync(partitionKey, workitemKey, WorkitemStoreStatus.ReadWrite, cancellationToken);
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

        public async Task UpdateWorkitemStatusAsync(int partitionKey, long workitemKey, WorkitemStoreStatus status, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.UpdateWorkitemStatus
                    .PopulateCommand(sqlCommandWrapper, partitionKey, workitemKey, (byte)status);

                try
                {
                    await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    throw new DataStoreException(ex);
                }
            }
        }

        public virtual async Task<WorkitemQueryResult> QueryAsync(
            int partitionKey,
            BaseQueryExpression query,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(query, nameof(query));

            var results = new List<WorkitemInstanceIdentifier>(query.EvaluatedLimit);

            using SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
            using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand();

            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var sqlQueryGenerator = new WorkitemSqlQueryGenerator(stringBuilder, query, new SqlQueryParameterManager(sqlCommandWrapper.Parameters), Version, partitionKey);

            sqlCommandWrapper.CommandText = stringBuilder.ToString();
            sqlCommandWrapper.LogSqlCommand(Logger);

            using SqlDataReader reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                (long workitemKey, string workitemInstanceUid, long watermark) = reader.ReadRow(
                   VLatest.Workitem.WorkitemKey,
                   VLatest.Workitem.WorkitemUid,
                   VLatest.Workitem.Watermark);

                results.Add(new WorkitemInstanceIdentifier(
                    workitemInstanceUid,
                    workitemKey,
                    watermark,
                    partitionKey));
            }

            return new WorkitemQueryResult(results);
        }

        public virtual Task<(long WorkitemKey, long Watermark)?> BeginAddWorkitemWithWatermarkAsync(int partitionKey, DicomDataset dataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken)
        {
            throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
        }

        public virtual Task UpdateWorkitemProcedureStepStateAsync(WorkitemMetadataStoreEntry workitemMetadata, long proposedWatermark, string procedureStepState, CancellationToken cancellationToken = default)
        {
            throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
        }

        public virtual Task<WorkitemMetadataStoreEntry> GetWorkitemMetadataAsync(int partitionKey, string workitemUid, CancellationToken cancellationToken = default)
        {
            throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
        }

        public virtual Task<(long CurrentWatermark, long NextWatermark)?> GetCurrentAndNextWorkitemWatermarkAsync(int partitionKey, string workitemUid, CancellationToken cancellationToken = default)
        {
            throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
        }
    }
}
