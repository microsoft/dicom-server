// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store;

/// <summary>
/// Sql IndexDataStore version 31.
/// </summary>
internal class SqlIndexDataStoreV31 : SqlIndexDataStoreV23
{
    protected static readonly Health.SqlServer.Features.Schema.Model.BigIntColumn ProposedWatermarkColumn =
        new Health.SqlServer.Features.Schema.Model.BigIntColumn("ProposedWatermark");

    public SqlIndexDataStoreV31(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        : base(sqlConnectionWrapperFactory)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V31;

    public async override Task<long> GetInstanceNextWatermark(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.GetNextInstanceWatermark
                .PopulateCommand(sqlCommandWrapper);

            using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    return reader.ReadRow(ProposedWatermarkColumn);
                }
            }
        }

        return 0;
    }

    public override async Task CreateInstanceRevision(VersionedInstanceIdentifier versionedInstanceIdentifier, long nextWatermark, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.AddInstanceRevision.PopulateCommand(
                sqlCommandWrapper,
                versionedInstanceIdentifier.PartitionKey,
                versionedInstanceIdentifier.StudyInstanceUid,
                versionedInstanceIdentifier.SeriesInstanceUid,
                versionedInstanceIdentifier.SopInstanceUid,
                versionedInstanceIdentifier.Revision,
                versionedInstanceIdentifier.Version,
                nextWatermark);

            try
            {
                await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                throw ex.Number switch
                {
                    SqlErrorCodes.NotFound => new InstanceNotFoundException(),
                    SqlErrorCodes.Conflict when ex.State == 10 => new ExtendedQueryTagsOutOfDateException(),
                    _ => new DataStoreException(ex),
                };
            }
        }
    }

    public override async Task UpdateStudyAsync(int partitionKey, DicomDataset dicomDataset, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.UpdateStudy.PopulateCommand(
                sqlCommandWrapper,
                partitionKey,
                dicomDataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                dicomDataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty),
                dicomDataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty),
                dicomDataset.GetSingleValueOrDefault(DicomTag.ReferringPhysicianName, string.Empty),
                dicomDataset.GetSingleValueOrDefault<DateTime>(DicomTag.StudyDate, DateTime.Now),
                dicomDataset.GetSingleValueOrDefault(DicomTag.StudyDescription, string.Empty),
                dicomDataset.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty));

            try
            {
                await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                throw ex.Number switch
                {
                    SqlErrorCodes.NotFound => new InstanceNotFoundException(),
                    SqlErrorCodes.Conflict when ex.State == 10 => new ExtendedQueryTagsOutOfDateException(),
                    _ => new DataStoreException(ex),
                };
            }
        }
    }
}
