// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Storage
{
    internal class DicomSqlIndexDataStore : IDicomIndexDataStore
    {
        private readonly DicomSqlIndexSchema _sqlServerDicomIndexSchema;
        private readonly ILogger<DicomSqlIndexDataStore> _logger;
        private readonly SqlConnectionWrapperFactory _sqlConnectionFactoryWrapper;

        public DicomSqlIndexDataStore(
            DicomSqlIndexSchema dicomIndexSchema,
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
            ILogger<DicomSqlIndexDataStore> logger)
        {
            EnsureArg.IsNotNull(dicomIndexSchema, nameof(dicomIndexSchema));
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _sqlServerDicomIndexSchema = dicomIndexSchema;
            _sqlConnectionFactoryWrapper = sqlConnectionWrapperFactory;
            _logger = logger;
        }

        public async Task DeleteInstanceIndexAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));
            EnsureArg.IsNotNullOrEmpty(sopInstanceUid, nameof(sopInstanceUid));

            await DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, cancellationToken);
        }

        public async Task DeleteSeriesIndexAsync(string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));

            await DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid: null, cancellationToken);
        }

        public async Task DeleteStudyIndexAsync(string studyInstanceUid, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));

            await DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid: null, sopInstanceUid: null, cancellationToken);
        }

        private async Task DeleteInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken = default)
        {
            await _sqlServerDicomIndexSchema.EnsureInitialized();

            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapper())
            using (SqlCommand sqlCommand = sqlConnectionWrapper.CreateSqlCommand())
            {
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

        public async Task<IEnumerable<VersionedDicomInstanceIdentifier>> RetrieveDeletedInstancesAsync(CancellationToken cancellationToken = default)
        {
            var results = new List<VersionedDicomInstanceIdentifier>();

            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapper())
            using (SqlCommand sqlCommand = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.RetrieveDeletedInstance.PopulateCommand(
                    sqlCommand,
                    Count: 1,
                    MaxRetries: 10);

                using (var reader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, long watermark) = reader.ReadRow(
                            VLatest.DeletedInstance.StudyInstanceUid,
                            VLatest.DeletedInstance.SeriesInstanceUid,
                            VLatest.DeletedInstance.SopInstanceUid,
                            VLatest.DeletedInstance.Watermark);

                        results.Add(new VersionedDicomInstanceIdentifier(
                            studyInstanceUid,
                            seriesInstanceUid,
                            sopInstanceUid,
                            watermark));
                    }
                }
            }

            return results;
        }

        public async Task DeleteDeletedInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, long watermark, CancellationToken cancellationToken = default)
        {
            await _sqlServerDicomIndexSchema.EnsureInitialized();

            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapper())
            using (SqlCommand sqlCommand = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.DeleteDeletedInstance.PopulateCommand(
                    sqlCommand,
                    studyInstanceUid,
                    seriesInstanceUid,
                    sopInstanceUid,
                    watermark);

                await sqlCommand.ExecuteScalarAsync(cancellationToken);
            }
        }

        public async Task IndexInstanceAsync(DicomDataset instance, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));

            await _sqlServerDicomIndexSchema.EnsureInitialized();

            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapper())
            using (SqlCommand sqlCommand = sqlConnectionWrapper.CreateSqlCommand())
            {
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
