// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store;

internal class SqlIndexDataStoreV47 : SqlIndexDataStoreV46
{
    public SqlIndexDataStoreV47(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        : base(sqlConnectionWrapperFactory)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V47;

    public override async Task<IReadOnlyList<InstanceMetadata>> RetrieveDeletedInstancesWithPropertiesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default)
    {
        var results = new List<InstanceMetadata>();

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken, true))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.RetrieveDeletedInstanceV42.PopulateCommand(
                sqlCommandWrapper,
                batchSize,
                maxRetries);

            using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
            {
                try
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (string partitionName, int partitionKey, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, long watermark, string filePath, string eTag) = reader.ReadRow(
                            VLatest.Partition.PartitionName,
                            VLatest.DeletedInstance.PartitionKey,
                            VLatest.DeletedInstance.StudyInstanceUid,
                            VLatest.DeletedInstance.SeriesInstanceUid,
                            VLatest.DeletedInstance.SopInstanceUid,
                            VLatest.DeletedInstance.Watermark,
                            VLatest.FileProperty.FilePath,
                            VLatest.FileProperty.ETag);

                        results.Add(
                        new InstanceMetadata(
                            new VersionedInstanceIdentifier(
                                studyInstanceUid,
                                seriesInstanceUid,
                                sopInstanceUid,
                                watermark,
                                new Partition(partitionKey, partitionName)),
                            new InstanceProperties()
                            {
                                fileProperties = new FileProperties
                                {
                                    Path = filePath,
                                    ETag = eTag
                                }
                            }));
                    }
                }
                catch (SqlException ex)
                {
                    throw new DataStoreException(ex);
                }
            }
        }

        return results;
    }

    private async Task DeleteInstanceAsync(int partitionKey, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.DeleteInstanceV47.PopulateCommand(
                sqlCommandWrapper,
                cleanupAfter,
                (byte)IndexStatus.Created,
                partitionKey,
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid);

            try
            {
                await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                switch (ex.Number)
                {
                    case SqlErrorCodes.NotFound:
                        if (!string.IsNullOrEmpty(sopInstanceUid))
                        {
                            throw new InstanceNotFoundException();
                        }

                        if (!string.IsNullOrEmpty(seriesInstanceUid))
                        {
                            throw new SeriesNotFoundException();
                        }

                        throw new StudyNotFoundException();

                    default:
                        throw new DataStoreException(ex);
                }
            }
        }
    }
}
