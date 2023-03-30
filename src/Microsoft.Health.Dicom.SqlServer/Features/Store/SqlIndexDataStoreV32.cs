// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store;

/// <summary>
/// Sql IndexDataStore version 32.
/// </summary>
internal class SqlIndexDataStoreV32 : SqlIndexDataStoreV23
{
    public SqlIndexDataStoreV32(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        : base(sqlConnectionWrapperFactory)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V32;

    public override async Task BulkUpdateStudyInstanceAsync(
        int batchSize,
        int partitionKey,
        string studyInstanceUid,
        DicomDataset dicomDataset,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.BulkUpdateStudyInstance.PopulateCommand(
                sqlCommandWrapper,
                batchSize,
                partitionKey,
                studyInstanceUid,
                dicomDataset.GetFirstValueOrDefault<string>(DicomTag.PatientID),
                dicomDataset.GetFirstValueOrDefault<string>(DicomTag.PatientName),
                dicomDataset.GetFirstValueOrDefault<string>(DicomTag.ReferringPhysicianName),
                dicomDataset.GetStringDateAsDate(DicomTag.StudyDate),
                dicomDataset.GetFirstValueOrDefault<string>(DicomTag.StudyDescription),
                dicomDataset.GetFirstValueOrDefault<string>(DicomTag.AccessionNumber),
                dicomDataset.GetStringDateAsDate(DicomTag.PatientBirthDate));

            try
            {
                await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                throw ex.Number switch
                {
                    SqlErrorCodes.NotFound => new StudyNotFoundException(),
                    _ => new DataStoreException(ex),
                };
            }
        }
    }

    public override async Task UpdateInstanceNewWatermarkAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.UpdateInstanceNewWatermark.PopulateCommand(
                sqlCommandWrapper,
                versionedInstanceIdentifier.PartitionKey,
                versionedInstanceIdentifier.StudyInstanceUid,
                versionedInstanceIdentifier.SeriesInstanceUid,
                versionedInstanceIdentifier.SopInstanceUid);

            try
            {
                await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                throw ex.Number switch
                {
                    SqlErrorCodes.NotFound => new InstanceNotFoundException(),
                    _ => new DataStoreException(ex),
                };
            }
        }
    }
}
