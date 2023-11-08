// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Partitioning;

internal class SqlPartitionStoreV6 : SqlPartitionStoreV4
{
    public SqlPartitionStoreV6(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
    {
        SqlConnectionWrapperFactory = EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
    }

    public override SchemaVersion Version => SchemaVersion.V6;

    protected SqlConnectionWrapperFactory SqlConnectionWrapperFactory { get; }

    public override async Task<Partition> AddPartitionAsync(string partitionName, CancellationToken cancellationToken)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.AddPartition.PopulateCommand(sqlCommandWrapper, partitionName);

            try
            {
                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        (int rPartitionKey, string rPartitionName, DateTimeOffset rCreatedDate) = reader.ReadRow(
                           VLatest.Partition.PartitionKey,
                           VLatest.Partition.PartitionName,
                           VLatest.Partition.CreatedDate);

                        return new Partition(
                            rPartitionKey,
                            rPartitionName,
                            rCreatedDate);
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == SqlErrorCodes.Conflict || ex.Number == SqlErrorCodes.UniqueKeyConstraintViolation)
                {
                    throw new DataPartitionAlreadyExistsException();
                }
            }
        }

        return null;
    }

    public override async Task<IEnumerable<Partition>> GetPartitionsAsync(CancellationToken cancellationToken)
    {
        var results = new List<Partition>();

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.GetPartitions.PopulateCommand(sqlCommandWrapper);

            using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    (int rPartitionKey, string rPartitionName, DateTimeOffset rCreatedDate) = reader.ReadRow(
                       VLatest.Partition.PartitionKey,
                       VLatest.Partition.PartitionName,
                       VLatest.Partition.CreatedDate);

                    results.Add(new Partition(
                        rPartitionKey,
                        rPartitionName,
                        rCreatedDate));
                }
            }

            return results;
        }
    }

    public override async Task<Partition> GetPartitionAsync(string partitionName, CancellationToken cancellationToken)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.GetPartition.PopulateCommand(sqlCommandWrapper, partitionName);

            using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
            {
                if (await reader.ReadAsync(cancellationToken))
                {
                    (int rPartitionKey, string rPartitionName, DateTimeOffset rCreatedDate) = reader.ReadRow(
                       VLatest.Partition.PartitionKey,
                       VLatest.Partition.PartitionName,
                       VLatest.Partition.CreatedDate);

                    return new Partition(
                        rPartitionKey,
                        rPartitionName,
                        rCreatedDate);
                }
            }

        }

        return null;
    }
}
