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
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Partition
{
    internal class SqlPartitionStoreV6 : SqlPartitionStoreV5
    {

        protected SqlConnectionWrapperFactory SqlConnectionWrapperFactory;

        public SqlPartitionStoreV6(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        {
            SqlConnectionWrapperFactory = EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
        }

        public override SchemaVersion Version => SchemaVersion.V6;

        public override async Task<PartitionEntry> AddPartition(string partitionName, CancellationToken cancellationToken)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.AddPartition.PopulateCommand(sqlCommandWrapper, partitionName);

                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        (int rPartitionKey, string rPartitionName, DateTimeOffset rCreatedDate) = reader.ReadRow(
                           VLatest.Partition.PartitionKey,
                           VLatest.Partition.PartitionName,
                           VLatest.Partition.CreatedDate);

                        return new PartitionEntry(
                            rPartitionKey,
                            rPartitionName,
                            rCreatedDate);
                    }
                }

            }

            return null;
        }

        public override async Task<IEnumerable<PartitionEntry>> GetPartitions(CancellationToken cancellationToken)
        {
            var results = new List<PartitionEntry>();

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
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

                        results.Add(new PartitionEntry(
                            rPartitionKey,
                            rPartitionName,
                            rCreatedDate));
                    }
                }

                return results;
            }
        }

        public override async Task<PartitionEntry> GetPartition(string partitionName, CancellationToken cancellationToken)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
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

                        return new PartitionEntry(
                            rPartitionKey,
                            rPartitionName,
                            rCreatedDate);
                    }
                }

            }

            return null;
        }
    }
}
