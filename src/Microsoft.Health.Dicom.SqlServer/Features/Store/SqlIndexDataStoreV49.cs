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
    { }

    public override SchemaVersion Version => SchemaVersion.V47;

    protected override async Task<IReadOnlyCollection<VersionedInstanceIdentifier>> DeleteInstanceAsync(Partition partition, string studyInstanceUid, string seriesInstanceUid,
        string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
    {
        var results = new List<VersionedInstanceIdentifier>();
        using SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
        using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();
        VLatest.DeleteInstanceV6.PopulateCommand(
            sqlCommandWrapper,
            cleanupAfter,
            (byte)IndexStatus.Created,
            partition.Key,
            studyInstanceUid,
            seriesInstanceUid,
            sopInstanceUid);

        try
        {
            using var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                (long rWatermark, int rpartition, string rStudyInstanceUid, string rSeriesInstanceUid, string rSopInstanceUid) = reader.ReadRow(
                    VLatest.DeletedInstance.Watermark,
                    VLatest.DeletedInstance.PartitionKey,
                    VLatest.DeletedInstance.StudyInstanceUid,
                    VLatest.DeletedInstance.SeriesInstanceUid,
                    VLatest.DeletedInstance.SopInstanceUid);
                results.Add(new VersionedInstanceIdentifier(rStudyInstanceUid, rSeriesInstanceUid, rSopInstanceUid, rWatermark, partition));
            }
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

        return results;
    }
}
