// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.SqlServer.Extensions;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store;

internal class SqlIndexDataStoreV49 : SqlIndexDataStoreV47
{
    public SqlIndexDataStoreV49(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        : base(sqlConnectionWrapperFactory)
    { }

    public override SchemaVersion Version => SchemaVersion.V49;

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
                        (string partitionName, int partitionKey, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, long watermark, long? originalWatermark, string filePath, string eTag) = reader.ReadRow(
                            VLatest.Partition.PartitionName,
                            VLatest.DeletedInstance.PartitionKey,
                            VLatest.DeletedInstance.StudyInstanceUid,
                            VLatest.DeletedInstance.SeriesInstanceUid,
                            VLatest.DeletedInstance.SopInstanceUid,
                            VLatest.DeletedInstance.Watermark,
                            VLatest.DeletedInstance.OriginalWatermark,
                            VLatest.FileProperty.ETag.AsNullable(),
                            VLatest.FileProperty.FilePath.AsNullable());

                        results.Add(
                            new InstanceMetadata(
                                new VersionedInstanceIdentifier(
                                    studyInstanceUid,
                                    seriesInstanceUid,
                                    sopInstanceUid,
                                    watermark,
                                    new Partition(partitionKey, partitionName)),
                                instanceProperties: CreateInstanceProperties(eTag, filePath, originalWatermark)
                            ));
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

    private static InstanceProperties CreateInstanceProperties(string eTag, string filePath, long? originalWatermark)
    {
        if (eTag != null && filePath != null)
        {
            return new InstanceProperties
            {
                OriginalVersion = originalWatermark,
                FileProperties = new FileProperties
                {
                    Path = filePath,
                    ETag = eTag
                }
            };
        }

        return new InstanceProperties();
    }
}