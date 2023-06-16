// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
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

internal class SqlInstanceStoreV10 : SqlInstanceStoreV6
{
    public SqlInstanceStoreV10(SqlConnectionWrapperFactory sqlConnectionWrapperFactory) : base(sqlConnectionWrapperFactory)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V10;

    public override async Task<IReadOnlyList<InstanceMetadata>> GetInstanceIdentifierWithPropertiesAsync(PartitionEntry partitionEntry, string studyInstanceUid, string seriesInstanceUid = null, string sopInstanceUid = null, CancellationToken cancellationToken = default)
    {
        var results = new List<InstanceMetadata>();

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.GetInstanceWithProperties.PopulateCommand(
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
                    (string rStudyInstanceUid, string rSeriesInstanceUid, string rSopInstanceUid, long watermark, string rTransferSyntaxUid) = reader.ReadRow(
                       VLatest.Instance.StudyInstanceUid,
                       VLatest.Instance.SeriesInstanceUid,
                       VLatest.Instance.SopInstanceUid,
                       VLatest.Instance.Watermark,
                       VLatest.Instance.TransferSyntaxUid);

                    results.Add(
                        new InstanceMetadata(
                            new VersionedInstanceIdentifier(
                                rStudyInstanceUid,
                                rSeriesInstanceUid,
                                rSopInstanceUid,
                                watermark,
                                partitionEntry),
                            new InstanceProperties() { TransferSyntaxUid = rTransferSyntaxUid }));
                }
            }
        }

        return results;
    }
}
