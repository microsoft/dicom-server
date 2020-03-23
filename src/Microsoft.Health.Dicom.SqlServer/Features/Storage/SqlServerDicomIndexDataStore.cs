// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Storage
{
    internal class SqlServerDicomIndexDataStore : IDicomIndexDataStore
    {
        private readonly SqlServerDicomIndexSchema _sqlServerDicomIndexSchema;
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
        private readonly ILogger<SqlServerDicomIndexDataStore> _logger;

        public SqlServerDicomIndexDataStore(
            SqlServerDicomIndexSchema dicomIndexSchema,
            SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration,
            ILogger<SqlServerDicomIndexDataStore> logger)
        {
            EnsureArg.IsNotNull(dicomIndexSchema, nameof(dicomIndexSchema));
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _sqlServerDicomIndexSchema = dicomIndexSchema;
            _sqlServerDataStoreConfiguration = sqlServerDataStoreConfiguration;
            _logger = logger;
        }

        public Task DeleteInstanceIndexAsync(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IEnumerable<DicomInstance>> DeleteSeriesIndexAsync(string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Enumerable.Empty<DicomInstance>());
        }

        public Task<IEnumerable<DicomInstance>> DeleteStudyIndexAsync(string studyInstanceUID, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Enumerable.Empty<DicomInstance>());
        }

        public async Task IndexInstanceAsync(DicomDataset instance, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));

            await _sqlServerDicomIndexSchema.EnsureInitialized();

            using (SqlConnection sqlConnection = new SqlConnection(_sqlServerDataStoreConfiguration.ConnectionString))
            using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
            {
                sqlCommand.Connection.Open();

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
                        // TODO: Update this to const once nuget is updated
                        case 50409:
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
