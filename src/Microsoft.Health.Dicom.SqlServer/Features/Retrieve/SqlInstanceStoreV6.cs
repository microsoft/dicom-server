// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Retrieve;

internal class SqlInstanceStoreV6 : SqlInstanceStoreV4
{

    public SqlInstanceStoreV6(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        : base(sqlConnectionWrapperFactory)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V6;

    public override Task<IReadOnlyList<VersionedInstanceIdentifier>> GetInstanceIdentifierAsync(
        PartitionEntry partitionEntry,
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid,
        CancellationToken cancellationToken)
    {
        return GetInstanceIdentifierImp(partitionEntry, studyInstanceUid, cancellationToken, seriesInstanceUid, sopInstanceUid);
    }

    public override Task<IReadOnlyList<VersionedInstanceIdentifier>> GetInstanceIdentifiersInSeriesAsync(
        PartitionEntry partitionEntry,
        string studyInstanceUid,
        string seriesInstanceUid,
        CancellationToken cancellationToken)
    {
        return GetInstanceIdentifierImp(partitionEntry, studyInstanceUid, cancellationToken, seriesInstanceUid);
    }

    public override Task<IReadOnlyList<VersionedInstanceIdentifier>> GetInstanceIdentifiersInStudyAsync(
        PartitionEntry partitionEntry,
        string studyInstanceUid,
        CancellationToken cancellationToken)
    {
        return GetInstanceIdentifierImp(partitionEntry, studyInstanceUid, cancellationToken);
    }

    public override async Task<IReadOnlyList<VersionedInstanceIdentifier>> GetInstanceIdentifiersByWatermarkRangeAsync(
        WatermarkRange watermarkRange,
        IndexStatus indexStatus,
        CancellationToken cancellationToken = default)
    {
        var results = new List<VersionedInstanceIdentifier>();

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.GetInstancesByWatermarkRangeV6.PopulateCommand(
                sqlCommandWrapper,
                watermarkRange.Start,
                watermarkRange.End,
                (byte)indexStatus);

            using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    (string rStudyInstanceUid, string rSeriesInstanceUid, string rSopInstanceUid, long watermark) = reader.ReadRow(
                       VLatest.Instance.StudyInstanceUid,
                       VLatest.Instance.SeriesInstanceUid,
                       VLatest.Instance.SopInstanceUid,
                       VLatest.Instance.Watermark);

                    results.Add(new VersionedInstanceIdentifier(
                        rStudyInstanceUid,
                        rSeriesInstanceUid,
                        rSopInstanceUid,
                        watermark));
                }
            }
        }

        return results;
    }


    public override async Task<IReadOnlyList<InstanceMetadata>> GetInstanceIdentifierWithPropertiesAsync(PartitionEntry partitionEntry, string studyInstanceUid, string seriesInstanceUid = null, string sopInstanceUid = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<VersionedInstanceIdentifier> indentifiers = await GetInstanceIdentifierImp(partitionEntry, studyInstanceUid, cancellationToken, seriesInstanceUid, sopInstanceUid);
        return indentifiers.Select(i => new InstanceMetadata(i, new InstanceProperties())).ToList();
    }

    private async Task<IReadOnlyList<VersionedInstanceIdentifier>> GetInstanceIdentifierImp(
        PartitionEntry partitionEntry,
        string studyInstanceUid,
        CancellationToken cancellationToken,
        string seriesInstanceUid = null,
        string sopInstanceUid = null)
    {
        var results = new List<VersionedInstanceIdentifier>();

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.GetInstanceV6.PopulateCommand(
                sqlCommandWrapper,
                validStatus: (byte)IndexStatus.Created,
                partitionEntry.PartitionKey,
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid);

            using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    (string rStudyInstanceUid, string rSeriesInstanceUid, string rSopInstanceUid, long watermark) = reader.ReadRow(
                       VLatest.Instance.StudyInstanceUid,
                       VLatest.Instance.SeriesInstanceUid,
                       VLatest.Instance.SopInstanceUid,
                       VLatest.Instance.Watermark);

                    results.Add(new VersionedInstanceIdentifier(
                            rStudyInstanceUid,
                            rSeriesInstanceUid,
                            rSopInstanceUid,
                            watermark,
                            partitionEntry));
                }
            }
        }

        return results;
    }
}
