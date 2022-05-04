// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store;

/// <summary>
/// Sql IndexDataStore version 6.
/// </summary>
internal class SqlIndexDataStoreV10 : SqlIndexDataStoreV6
{
    public SqlIndexDataStoreV10(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        : base(sqlConnectionWrapperFactory)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V10;

    public override async Task<long> BeginCreateInstanceIndexAsync(int partitionKey, DicomDataset instance, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(instance, nameof(instance));
        EnsureArg.IsNotNull(queryTags, nameof(queryTags));

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            var rows = ExtendedQueryTagDataRowsBuilder.Build(instance, queryTags.Where(tag => tag.IsExtendedQueryTag), Version);
            VLatest.AddInstanceV6TableValuedParameters parameters = new VLatest.AddInstanceV6TableValuedParameters(
                rows.StringRows,
                rows.LongRows,
                rows.DoubleRows,
                rows.DateTimeWithUtcRows,
                rows.PersonNameRows
            );

            VLatest.AddInstanceV6.PopulateCommand(
                sqlCommandWrapper,
                partitionKey,
                instance.GetString(DicomTag.StudyInstanceUID),
                instance.GetString(DicomTag.SeriesInstanceUID),
                instance.GetString(DicomTag.SOPInstanceUID),
                instance.GetFirstValueOrDefault<string>(DicomTag.PatientID),
                instance.GetFirstValueOrDefault<string>(DicomTag.PatientName),
                instance.GetFirstValueOrDefault<string>(DicomTag.ReferringPhysicianName),
                instance.GetStringDateAsDate(DicomTag.StudyDate),
                instance.GetFirstValueOrDefault<string>(DicomTag.StudyDescription),
                instance.GetFirstValueOrDefault<string>(DicomTag.AccessionNumber),
                instance.GetFirstValueOrDefault<string>(DicomTag.Modality),
                instance.GetStringDateAsDate(DicomTag.PerformedProcedureStepStartDate),
                instance.GetStringDateAsDate(DicomTag.PatientBirthDate),
                instance.GetFirstValueOrDefault<string>(DicomTag.ManufacturerModelName),
                (byte)IndexStatus.Creating,
                instance.InternalTransferSyntax?.UID.UID,
                parameters);

            try
            {
                return (long)(await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken));
            }
            catch (SqlException ex) when (ex.Number == SqlErrorCodes.Conflict && ex.State == (byte)IndexStatus.Creating)
            {
                throw new PendingInstanceException();
            }
            catch (SqlException ex) when (ex.Number == SqlErrorCodes.Conflict)
            {
                throw new InstanceAlreadyExistsException();
            }
            catch (SqlException ex)
            {
                throw new DataStoreException(ex);
            }
        }
    }
}
