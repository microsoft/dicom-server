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
using Microsoft.Health.Dicom.Core;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
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

namespace Microsoft.Health.Dicom.SqlServer.Features.Workitem;
internal class SqlWorkitemStoreV22 : SqlWorkitemStoreV9
{
    protected static readonly Health.SqlServer.Features.Schema.Model.NVarCharColumn ProcedureStepStateColumn =
        new Health.SqlServer.Features.Schema.Model.NVarCharColumn("ProcedureStepState", 64);

    protected static readonly Health.SqlServer.Features.Schema.Model.BigIntColumn ProposedWatermarkColumn =
        new Health.SqlServer.Features.Schema.Model.BigIntColumn("ProposedWatermark");

    public SqlWorkitemStoreV22(SqlConnectionWrapperFactory sqlConnectionWrapperFactory, ILogger<ISqlWorkitemStore> logger)
        : base(sqlConnectionWrapperFactory, logger)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V22;

    public override async Task<WorkitemInstanceIdentifier> BeginAddWorkitemAsync(int partitionKey, DicomDataset dataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            var rows = ExtendedQueryTagDataRowsBuilder.Build(dataset, queryTags, Version);
            var parameters = new VLatest.AddWorkitemV11TableValuedParameters(
                rows.StringRows,
                rows.DateTimeWithUtcRows,
                rows.PersonNameRows
            );

            string workitemUid = dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);

            VLatest.AddWorkitemV11.PopulateCommand(
                sqlCommandWrapper,
                partitionKey,
                workitemUid,
                (byte)IndexStatus.Creating,
                parameters);

            try
            {
                using SqlDataReader reader = await sqlCommandWrapper
                    .ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    (long workitemKey, long watermark) = reader.ReadRow(
                       VLatest.Workitem.WorkitemKey,
                       VLatest.Workitem.Watermark);

                    return new WorkitemInstanceIdentifier(workitemUid, workitemKey, partitionKey, watermark);
                }

                throw new DataStoreException(DicomCoreResource.DataStoreOperationFailed);
            }
            catch (SqlException ex)
            {
                if (ex.Number == SqlErrorCodes.Conflict)
                {
                    throw new WorkitemAlreadyExistsException();
                }

                throw new DataStoreException(ex);
            }
        }
    }

    public override async Task<WorkitemQueryResult> QueryAsync(
        int partitionKey,
        BaseQueryExpression query,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(query, nameof(query));

        var results = new List<WorkitemInstanceIdentifier>(query.EvaluatedLimit);

        using SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
        using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();

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
                partitionKey,
                watermark));
        }

        return new WorkitemQueryResult(results);
    }

    public override async Task<WorkitemMetadataStoreEntry> GetWorkitemMetadataAsync(
        int partitionKey,
        string workitemUid,
        CancellationToken cancellationToken = default)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            var procedureStepStateTagPath = DicomTag.ProcedureStepState.GetPath();

            VLatest.GetWorkitemMetadata.PopulateCommand(sqlCommandWrapper, partitionKey, workitemUid, procedureStepStateTagPath);

            using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    (
                        string wiUid,
                        long wiKey,
                        int pkey,
                        byte status,
                        string transactionUid,
                        long watermark,
                        string procedureStepState
                    ) = reader.ReadRow(
                            VLatest.Workitem.WorkitemUid,
                            VLatest.Workitem.WorkitemKey,
                            VLatest.Workitem.PartitionKey,
                            VLatest.Workitem.Status,
                            VLatest.Workitem.TransactionUid,
                            VLatest.Workitem.Watermark,
                            ProcedureStepStateColumn);

                    return new WorkitemMetadataStoreEntry(wiUid, wiKey, watermark, pkey)
                    {
                        Status = (WorkitemStoreStatus)status,
                        TransactionUid = transactionUid,
                        ProcedureStepState = ProcedureStepStateExtensions.GetProcedureStepState(procedureStepState)
                    };
                }
            }
        }

        return null;
    }

    public override async Task<(long CurrentWatermark, long NextWatermark)?> GetCurrentAndNextWorkitemWatermarkAsync(
        long workitemKey,
        CancellationToken cancellationToken = default)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            var procedureStepStateTagPath = DicomTag.ProcedureStepState.GetPath();

            VLatest.GetCurrentAndNextWorkitemWatermark
                .PopulateCommand(sqlCommandWrapper, workitemKey);

            using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    return reader.ReadRow(
                        VLatest.Workitem.Watermark,
                        ProposedWatermarkColumn);
                }
            }
        }

        return null;
    }

    public override async Task UpdateWorkitemProcedureStepStateAsync(
        WorkitemMetadataStoreEntry workitemMetadata,
        long proposedWatermark,
        string procedureStepState,
        string transactionUid,
        CancellationToken cancellationToken = default)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.UpdateWorkitemProcedureStepStateV21.PopulateCommand(
                sqlCommandWrapper,
                workitemMetadata.WorkitemKey,
                DicomTag.ProcedureStepState.GetPath(),
                procedureStepState,
                workitemMetadata.Watermark,
                proposedWatermark,
                transactionUid);

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

    public override async Task UpdateWorkitemTransactionAsync(
        WorkitemMetadataStoreEntry workitemMetadata,
        long proposedWatermark,
        DicomDataset dataset,
        IEnumerable<QueryTag> queryTags,
        CancellationToken cancellationToken = default)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            var rows = ExtendedQueryTagDataRowsBuilder.Build(dataset, queryTags, Version);
            var parameters = new VLatest.UpdateWorkitemTransactionTableValuedParameters(
                rows.StringRows,
                rows.DateTimeWithUtcRows,
                rows.PersonNameRows
            );

            string workitemUid = workitemMetadata.WorkitemUid;

            VLatest.UpdateWorkitemTransaction.PopulateCommand(
                sqlCommandWrapper,
                workitemMetadata.WorkitemKey,
                workitemMetadata.PartitionKey,
                workitemMetadata.Watermark,
                proposedWatermark,
                parameters);

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
}
