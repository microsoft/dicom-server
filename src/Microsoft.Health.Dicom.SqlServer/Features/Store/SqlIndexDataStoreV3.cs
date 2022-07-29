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
/// Sql IndexDataStore version 3.
/// </summary>
internal class SqlIndexDataStoreV3 : SqlIndexDataStoreV2
{
    public SqlIndexDataStoreV3(
        SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        : base(sqlConnectionWrapperFactory)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V3;

    public override async Task<long> BeginCreateInstanceIndexAsync(int partitionKey, DicomDataset dicomDataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
        EnsureArg.IsNotNull(queryTags, nameof(queryTags));

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            var rows = ExtendedQueryTagDataRowsBuilder.Build(dicomDataset, queryTags.Where(tag => tag.IsExtendedQueryTag), Version);
            V3.AddInstanceTableValuedParameters parameters = new V3.AddInstanceTableValuedParameters(
                rows.StringRows,
                rows.LongRows,
                rows.DoubleRows,
                rows.DateTimeRows,
                rows.PersonNameRows
            );

            V3.AddInstance.PopulateCommand(
                sqlCommandWrapper,
                dicomDataset.GetString(DicomTag.StudyInstanceUID),
                dicomDataset.GetString(DicomTag.SeriesInstanceUID),
                dicomDataset.GetString(DicomTag.SOPInstanceUID),
                dicomDataset.GetFirstValueOrDefault<string>(DicomTag.PatientID),
                dicomDataset.GetFirstValueOrDefault<string>(DicomTag.PatientName),
                dicomDataset.GetFirstValueOrDefault<string>(DicomTag.ReferringPhysicianName),
                dicomDataset.GetStringDateAsDate(DicomTag.StudyDate),
                dicomDataset.GetFirstValueOrDefault<string>(DicomTag.StudyDescription),
                dicomDataset.GetFirstValueOrDefault<string>(DicomTag.AccessionNumber),
                dicomDataset.GetFirstValueOrDefault<string>(DicomTag.Modality),
                dicomDataset.GetStringDateAsDate(DicomTag.PerformedProcedureStepStartDate),
                dicomDataset.GetStringDateAsDate(DicomTag.PatientBirthDate),
                dicomDataset.GetFirstValueOrDefault<string>(DicomTag.ManufacturerModelName),
                (byte)IndexStatus.Creating,
                parameters);

            try
            {
                return (long)(await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken));
            }
            catch (SqlException ex)
            {
                if (ex.Number == SqlErrorCodes.Conflict)
                {
                    if (ex.State == (byte)IndexStatus.Creating)
                    {
                        throw new PendingInstanceException();
                    }

                    throw new InstanceAlreadyExistsException();
                }

                throw new DataStoreException(ex);
            }
        }
    }
}
