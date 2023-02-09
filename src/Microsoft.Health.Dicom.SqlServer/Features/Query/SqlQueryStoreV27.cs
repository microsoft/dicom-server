// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using System.Collections.Generic;
using System.Data;
using System;
using Microsoft.Health.SqlServer.Features.Schema.Model;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using System.Linq;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Query;

internal class SqlQueryStoreV27 : SqlQueryStoreV6
{
    public override SchemaVersion Version => SchemaVersion.V27;

    public SqlQueryStoreV27(
        SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
        ILogger<ISqlQueryStore> logger)
        : base(sqlConnectionWrapperFactory, logger)
    {
    }

    public override async Task<IReadOnlyCollection<StudyResult>> GetStudyResultAsync(
        int partitionKey,
        IReadOnlyCollection<long> versions,
        CancellationToken cancellationToken)
    {
        var results = new List<StudyResult>();

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            List<WatermarkTableTypeRow> versionRows = versions.Select(i => new WatermarkTableTypeRow(i)).ToList();
            VLatest.GetStudyResult.PopulateCommand(
                sqlCommandWrapper,
                partitionKey,
                versionRows);

            using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    (string rStudyInstanceUid,
                    string rPatientId,
                    string rPatientName,
                    string rReferringPhysicianName,
                    DateTime? rStudyDate,
                    string rStudyDescription,
                    string rAccessionNumber,
                    DateTime? rPatientBirthDate,
                    string rModalitiesInStudy,
                    int rNumberofStudyRelatedInstances
                     ) = reader.ReadRow(
                       VLatest.Study.StudyInstanceUid,
                       VLatest.Study.PatientId,
                       VLatest.Study.PatientName,
                       VLatest.Study.ReferringPhysicianName,
                       VLatest.Study.StudyDate,
                       VLatest.Study.StudyDescription,
                       VLatest.Study.AccessionNumber,
                       VLatest.Study.PatientBirthDate,
                       new NVarCharColumn("ModalitiesInStudy", 4000),
                       new IntColumn("NumberofStudyRelatedInstances"));

                    results.Add(new StudyResult()
                    {
                        StudyInstanceUid = rStudyInstanceUid,
                        PatientId = rPatientId,
                        PatientName = rPatientName,
                        ReferringPhysicianName = rReferringPhysicianName,
                        StudyDate = rStudyDate,
                        StudyDescription = rStudyDescription,
                        AccessionNumber = rAccessionNumber,
                        PatientBirthDate = rPatientBirthDate,
                        ModalitiesInStudy = rModalitiesInStudy?.Split(',', StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray(),
                        NumberofStudyRelatedInstances = rNumberofStudyRelatedInstances
                    });
                }
            }
        }
        return results;
    }

    public override async Task<IReadOnlyCollection<SeriesResult>> GetSeriesResultAsync(
      int partitionKey,
      IReadOnlyCollection<long> versions,
      CancellationToken cancellationToken)
    {
        var results = new List<SeriesResult>();

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            List<WatermarkTableTypeRow> versionRows = versions.Select(i => new WatermarkTableTypeRow(i)).ToList();
            VLatest.GetSeriesResult.PopulateCommand(
                sqlCommandWrapper,
                partitionKey,
                versionRows);

            using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    (string rStudyInstanceUid,
                     string rSeriesInstanceUid,
                     string rModality,
                     DateTime? rPerformedProcedureStepStartDate,
                     string rManufacturerModelName,
                     int rNumberofSeriesRelatedInstances
                     ) = reader.ReadRow(
                       VLatest.Instance.StudyInstanceUid,
                       VLatest.Series.SeriesInstanceUid,
                       VLatest.Series.Modality,
                       VLatest.Series.PerformedProcedureStepStartDate,
                       VLatest.Series.ManufacturerModelName,
                       new IntColumn("NumberofSeriesRelatedInstances"));

                    results.Add(new SeriesResult()
                    {
                        StudyInstanceUid = rStudyInstanceUid,
                        SeriesInstanceUid = rSeriesInstanceUid,
                        Modality = rModality,
                        PerformedProcedureStepStartDate = rPerformedProcedureStepStartDate,
                        ManufacturerModelName = rManufacturerModelName,
                        NumberOfSeriesRelatedInstances = rNumberofSeriesRelatedInstances,
                    });
                }
            }
        }
        return results;
    }
}
