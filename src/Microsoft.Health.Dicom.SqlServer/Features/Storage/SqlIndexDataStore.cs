// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Storage
{
    internal class SqlIndexDataStore : IIndexDataStore
    {
        private readonly SqlIndexSchema _sqlServerIndexSchema;
        private readonly ILogger<SqlIndexDataStore> _logger;
        private readonly SqlConnectionWrapperFactory _sqlConnectionFactoryWrapper;

        public SqlIndexDataStore(
            SqlIndexSchema indexSchema,
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
            ILogger<SqlIndexDataStore> logger)
        {
            EnsureArg.IsNotNull(indexSchema, nameof(indexSchema));
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _sqlServerIndexSchema = indexSchema;
            _sqlConnectionFactoryWrapper = sqlConnectionWrapperFactory;
            _logger = logger;
        }

        public async Task<long> CreateInstanceIndexAsync(DicomDataset instance, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));

            await _sqlServerIndexSchema.EnsureInitialized();

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
                    (byte)IndexStatus.Creating);

                try
                {
                    return (long)(await sqlCommand.ExecuteScalarAsync(cancellationToken));
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlErrorCodes.Conflict:
                            throw new InstanceAlreadyExistsException();

                        default:
                            _logger.LogError(ex, $"Error from SQL database on {nameof(VLatest.AddInstance)}.");
                            throw;
                    }
                }
            }
        }

        public async Task DeleteInstanceIndexAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));
            EnsureArg.IsNotNullOrEmpty(sopInstanceUid, nameof(sopInstanceUid));

            await DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, cleanupAfter, cancellationToken);
        }

        public async Task DeleteSeriesIndexAsync(string studyInstanceUid, string seriesInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));

            await DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid: null, cleanupAfter, cancellationToken);
        }

        public async Task DeleteStudyIndexAsync(string studyInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));

            await DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid: null, sopInstanceUid: null, cleanupAfter, cancellationToken);
        }

        public async Task UpdateInstanceIndexStatusAsync(
            VersionedInstanceIdentifier instanceIdentifier,
            IndexStatus status,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));
            EnsureArg.IsTrue(Enum.IsDefined(typeof(IndexStatus), status));
            EnsureArg.IsTrue((int)status < byte.MaxValue);

            await _sqlServerIndexSchema.EnsureInitialized();

            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapper())
            using (SqlCommand sqlCommand = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.UpdateInstanceStatus.PopulateCommand(
                    sqlCommand,
                    instanceIdentifier.StudyInstanceUid,
                    instanceIdentifier.SeriesInstanceUid,
                    instanceIdentifier.SopInstanceUid,
                    instanceIdentifier.Version,
                    (byte)status);

                try
                {
                    await sqlCommand.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlErrorCodes.NotFound:
                            throw new InstanceNotFoundException();

                        default:
                            _logger.LogError(ex, $"Error from SQL database on {nameof(VLatest.AddInstance)}.");
                            throw;
                    }
                }
            }
        }

        public async Task<IEnumerable<VersionedInstanceIdentifier>> RetrieveDeletedInstancesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default)
        {
            var results = new List<VersionedInstanceIdentifier>();

            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapper(true))
            using (SqlCommand sqlCommand = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.RetrieveDeletedInstance.PopulateCommand(
                    sqlCommand,
                    batchSize,
                    maxRetries);

                using (var reader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, long watermark) = reader.ReadRow(
                            VLatest.DeletedInstance.StudyInstanceUid,
                            VLatest.DeletedInstance.SeriesInstanceUid,
                            VLatest.DeletedInstance.SopInstanceUid,
                            VLatest.DeletedInstance.Watermark);

                        results.Add(new VersionedInstanceIdentifier(
                            studyInstanceUid,
                            seriesInstanceUid,
                            sopInstanceUid,
                            watermark));
                    }
                }
            }

            return results;
        }

        public async Task DeleteDeletedInstanceAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default)
        {
            await _sqlServerIndexSchema.EnsureInitialized();

            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapper(true))
            using (SqlCommand sqlCommand = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.DeleteDeletedInstance.PopulateCommand(
                    sqlCommand,
                    versionedInstanceIdentifier.StudyInstanceUid,
                    versionedInstanceIdentifier.SeriesInstanceUid,
                    versionedInstanceIdentifier.SopInstanceUid,
                    versionedInstanceIdentifier.Version);

                await sqlCommand.ExecuteScalarAsync(cancellationToken);
            }
        }

        public async Task<int> IncrementDeletedInstanceRetryAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default)
        {
            await _sqlServerIndexSchema.EnsureInitialized();

            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapper(true))
            using (SqlCommand sqlCommand = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.IncrementDeletedInstanceRetry.PopulateCommand(
                    sqlCommand,
                    versionedInstanceIdentifier.StudyInstanceUid,
                    versionedInstanceIdentifier.SeriesInstanceUid,
                    versionedInstanceIdentifier.SopInstanceUid,
                    versionedInstanceIdentifier.Version,
                    cleanupAfter);

                return (int)(await sqlCommand.ExecuteScalarAsync(cancellationToken));
            }
        }

        private async Task DeleteInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            await _sqlServerIndexSchema.EnsureInitialized();

            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapper())
            using (SqlCommand sqlCommand = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.DeleteInstance.PopulateCommand(
                    sqlCommand,
                    cleanupAfter,
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
                            throw new InstanceNotFoundException();
                        default:
                            _logger.LogError(ex, $"Error from SQL database on {nameof(VLatest.DeleteInstance)}.");
                            throw;
                    }
                }
            }
        }
    }
}
