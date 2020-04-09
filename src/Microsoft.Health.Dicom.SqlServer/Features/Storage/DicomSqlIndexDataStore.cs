// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Storage
{
    internal class DicomSqlIndexDataStore : IDicomIndexDataStore
    {
        private readonly DicomSqlIndexSchema _sqlServerDicomIndexSchema;
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
        private readonly ILogger<DicomSqlIndexDataStore> _logger;

        public DicomSqlIndexDataStore(
            DicomSqlIndexSchema dicomIndexSchema,
            SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration,
            ILogger<DicomSqlIndexDataStore> logger)
        {
            EnsureArg.IsNotNull(dicomIndexSchema, nameof(dicomIndexSchema));
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _sqlServerDicomIndexSchema = dicomIndexSchema;
            _sqlServerDataStoreConfiguration = sqlServerDataStoreConfiguration;
            _logger = logger;
        }

        public async Task DeleteInstanceIndexAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));
            EnsureArg.IsNotNullOrEmpty(sopInstanceUid, nameof(sopInstanceUid));

            await DeleteInstanceAysnc(studyInstanceUid, seriesInstanceUid, sopInstanceUid, cancellationToken);
        }

        public async Task DeleteSeriesIndexAsync(string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));

            await DeleteInstanceAysnc(studyInstanceUid, seriesInstanceUid, sopInstanceUid: null, cancellationToken);
        }

        public async Task DeleteStudyIndexAsync(string studyInstanceUid, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));

            await DeleteInstanceAysnc(studyInstanceUid, seriesInstanceUid: null, sopInstanceUid: null, cancellationToken);
        }

        private async Task DeleteInstanceAysnc(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken = default)
        {
            await _sqlServerDicomIndexSchema.EnsureInitialized();

            using (var sqlConnection = new SqlConnection(_sqlServerDataStoreConfiguration.ConnectionString))
            using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
            {
                await sqlCommand.Connection.OpenAsync(cancellationToken);

                VLatest.DeleteInstance.PopulateCommand(
                    sqlCommand,
                    studyInstanceUid,
                    seriesInstanceUid,
                    sopInstanceUid);

                try
                {
                    await sqlCommand.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlErrorCodes.NotFound:
                            throw new DicomInstanceNotFoundException();
                        default:
                            _logger.LogError(ex, $"Error from SQL database on {nameof(VLatest.DeleteInstance)}.");
                            throw;
                    }
                }
            }
        }

        public async Task IndexInstanceAsync(DicomDataset instance, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));

            await _sqlServerDicomIndexSchema.EnsureInitialized();

            using (SqlConnection sqlConnection = new SqlConnection(_sqlServerDataStoreConfiguration.ConnectionString))
            using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
            {
                await sqlCommand.Connection.OpenAsync(cancellationToken);

                VLatest.AddInstance.PopulateCommand(
                    sqlCommand,
                    instance.GetString(DicomTag.StudyInstanceUID),
                    instance.GetString(DicomTag.SeriesInstanceUID),
                    instance.GetString(DicomTag.SOPInstanceUID),
                    instance.GetSingleValueOrDefault<string>(DicomTag.PatientID),
                    instance.GetSingleValueOrDefault<string>(DicomTag.PatientName),
                    instance.GetSingleValueOrDefault<string>(DicomTag.ReferringPhysicianName),
                    instance.GetStringDateAsDateTime(DicomTag.StudyDate),
                    instance.GetSingleValueOrDefault<string>(DicomTag.StudyDescription),
                    instance.GetSingleValueOrDefault<string>(DicomTag.AccessionNumber),
                    instance.GetSingleValueOrDefault<string>(DicomTag.Modality),
                    instance.GetStringDateAsDateTime(DicomTag.PerformedProcedureStepStartDate),
                    DicomIndexStatus.Created);

                try
                {
                    await sqlCommand.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlErrorCodes.Conflict:
                            throw new DicomInstanceAlreadyExistsException();

                        default:
                            _logger.LogError(ex, $"Error from SQL database on {nameof(VLatest.AddInstance)}.");
                            throw;
                    }
                }
            }
        }
    }
}
