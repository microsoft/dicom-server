// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.Dicom.SqlServer.Features.Retrieve;

internal class SqlInstanceStoreV48 : SqlInstanceStoreV46
{
    public SqlInstanceStoreV48(SqlConnectionWrapperFactory sqlConnectionWrapperFactory) : base(sqlConnectionWrapperFactory)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V48;

    public override async Task<IReadOnlyList<WatermarkRange>> GetInstanceBatchesByTimeStampAsync(
        int batchSize,
        int batchCount,
        IndexStatus indexStatus,
        DateTimeOffset startTimeStamp,
        DateTimeOffset endTimeStamp,
        long? maxWatermark = null,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsGt(batchSize, 0, nameof(batchSize));
        EnsureArg.IsGt(batchCount, 0, nameof(batchCount));

        using SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
        using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();

        VLatest.GetInstanceBatchesByTimeStamp.PopulateCommand(sqlCommandWrapper, batchSize, batchCount, (byte)indexStatus, startTimeStamp, endTimeStamp, maxWatermark);

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
}
