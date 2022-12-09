// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Partition;

internal class SqlPartitionStoreV25 : SqlPartitionStoreV6
{
    public SqlPartitionStoreV25(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        : base(sqlConnectionWrapperFactory)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V25;

    public override async Task<PartitionEntry> AddPartitionAsync(string partitionName, CancellationToken cancellationToken)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.AddPartitionV25.PopulateCommand(sqlCommandWrapper, partitionName);

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

                        return new PartitionEntry(
                            rPartitionKey,
                            rPartitionName,
                            rCreatedDate);
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == SqlErrorCodes.Conflict)
                {
                    throw new DataPartitionsAlreadyExistsException();
                }
            }
        }

        return null;
    }
}
