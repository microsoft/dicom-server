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
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Retrieve;

internal class SqlInstanceStoreV55 : SqlInstanceStoreV48
{
    public SqlInstanceStoreV55(SqlConnectionWrapperFactory sqlConnectionWrapperFactory) : base(sqlConnectionWrapperFactory)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V55;

    public override async Task<IReadOnlyList<WatermarkRange>> GetContentLengthBackFillInstanceBatches(
        int batchSize,
        int batchCount,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsGt(batchSize, 0, nameof(batchSize));
        EnsureArg.IsGt(batchCount, 0, nameof(batchCount));

        using SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
        using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();

        VLatest.GetContentLengthBackFillInstanceBatches.PopulateCommand(sqlCommandWrapper, batchSize, batchCount);

        try
        {
            var batches = new List<WatermarkRange>();
            using SqlDataReader reader = await sqlCommandWrapper.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                batches.Add(new WatermarkRange(reader.GetInt64(0), reader.GetInt64(1)));
            }

            return batches;
        }
        catch (SqlException ex)
        {
            throw new DataStoreException(ex);
        }
    }

    public override async Task<IReadOnlyList<VersionedInstanceIdentifier>> GetContentLengthBackFillInstanceIdentifiersByWatermarkRangeAsync(
        WatermarkRange watermarkRange,
        CancellationToken cancellationToken = default)
    {
        var results = new List<VersionedInstanceIdentifier>();

        try
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
            {
                VLatest.GetContentLengthBackFillInstanceIdentifiersByWatermarkRange.PopulateCommand(
                    sqlCommandWrapper,
                    watermarkRange.Start,
                    watermarkRange.End);

                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (string rStudyInstanceUid, string rSeriesInstanceUid, string rSopInstanceUid, long watermark, string partitionName, int partitionKey) = reader.ReadRow(
                            VLatest.Instance.StudyInstanceUid,
                            VLatest.Instance.SeriesInstanceUid,
                            VLatest.Instance.SopInstanceUid,
                            VLatest.Instance.Watermark,
                            VLatest.Partition.PartitionName,
                            VLatest.Partition.PartitionKey);

                        results.Add(new VersionedInstanceIdentifier(
                            rStudyInstanceUid,
                            rSeriesInstanceUid,
                            rSopInstanceUid,
                            watermark,
                            partition: new Partition(partitionKey, partitionName)));
                    }
                }
            }

        }
        catch (SqlException ex)
        {
            throw new DataStoreException(ex);
        }

        return results;
    }
}
