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
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.Dicom.SqlServer.Features.Storage;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    internal class DicomSqlIndexDataStore : IDicomIndexDataStore
    {
        private readonly DicomSqlIndexSchema _sqlServerDicomIndexSchema;
        private readonly SqlConnectionWrapperFactory _sqlConnectionFactoryWrapper;

        public DicomSqlIndexDataStore(
            DicomSqlIndexSchema dicomIndexSchema,
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        {
            EnsureArg.IsNotNull(dicomIndexSchema, nameof(dicomIndexSchema));
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));

            _sqlServerDicomIndexSchema = dicomIndexSchema;
            _sqlConnectionFactoryWrapper = sqlConnectionWrapperFactory;
        }

        public async Task<long> CreateInstanceIndexAsync(DicomDataset instance, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));

            await _sqlServerDicomIndexSchema.EnsureInitialized();

            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapper())
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.AddInstance.PopulateCommand(
                    sqlCommandWrapper,
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
                    (byte)DicomIndexStatus.Creating);

                try
                {
                    return (long)(await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken));
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlErrorCodes.Conflict:
                            {
                                if (ex.State == (byte)DicomIndexStatus.Creating)
                                {
                                    throw new PendingDicomInstanceException();
                                }

                                throw new DicomInstanceAlreadyExistsException();
                            }
                    }

                    throw new DicomDataStoreException(ex);
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
            VersionedDicomInstanceIdentifier dicomInstanceIdentifier,
            DicomIndexStatus status,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dicomInstanceIdentifier, nameof(dicomInstanceIdentifier));
            EnsureArg.IsTrue(Enum.IsDefined(typeof(DicomIndexStatus), status));
            EnsureArg.IsTrue((int)status < byte.MaxValue);

            await _sqlServerDicomIndexSchema.EnsureInitialized();

            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapper())
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.UpdateInstanceStatus.PopulateCommand(
                    sqlCommandWrapper,
                    dicomInstanceIdentifier.StudyInstanceUid,
                    dicomInstanceIdentifier.SeriesInstanceUid,
                    dicomInstanceIdentifier.SopInstanceUid,
                    dicomInstanceIdentifier.Version,
                    (byte)status);

                try
                {
                    await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlErrorCodes.NotFound:
                            throw new DicomInstanceNotFoundException();

                        default:
                            throw new DicomDataStoreException(ex);
                    }
                }
            }
        }

        public async Task<IEnumerable<VersionedDicomInstanceIdentifier>> RetrieveDeletedInstancesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default)
        {
            var results = new List<VersionedDicomInstanceIdentifier>();

            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapper(true))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.RetrieveDeletedInstance.PopulateCommand(
                    sqlCommandWrapper,
                    batchSize,
                    maxRetries);

                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    try
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
                    catch (SqlException ex)
                    {
                        throw new DicomDataStoreException(ex);
                    }
                }
            }

            return results;
        }

        public async Task DeleteDeletedInstanceAsync(VersionedDicomInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default)
        {
            await _sqlServerDicomIndexSchema.EnsureInitialized();

            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapper(true))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.DeleteDeletedInstance.PopulateCommand(
                    sqlCommandWrapper,
                    versionedInstanceIdentifier.StudyInstanceUid,
                    versionedInstanceIdentifier.SeriesInstanceUid,
                    versionedInstanceIdentifier.SopInstanceUid,
                    versionedInstanceIdentifier.Version);

                try
                {
                    await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    throw new DicomDataStoreException(ex);
                }
            }
        }

        public async Task<int> IncrementDeletedInstanceRetryAsync(VersionedDicomInstanceIdentifier versionedInstanceIdentifier, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default)
        {
            await _sqlServerDicomIndexSchema.EnsureInitialized();

            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapper(true))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.IncrementDeletedInstanceRetry.PopulateCommand(
                    sqlCommandWrapper,
                    versionedInstanceIdentifier.StudyInstanceUid,
                    versionedInstanceIdentifier.SeriesInstanceUid,
                    versionedInstanceIdentifier.SopInstanceUid,
                    versionedInstanceIdentifier.Version,
                    cleanupAfter);

                try
                {
                    return (int)(await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken));
                }
                catch (SqlException ex)
                {
                    throw new DicomDataStoreException(ex);
                }
            }
        }

        private async Task DeleteInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            await _sqlServerDicomIndexSchema.EnsureInitialized();

            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapper())
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.DeleteInstance.PopulateCommand(
                    sqlCommandWrapper,
                    cleanupAfter,
                    studyInstanceUid,
                    seriesInstanceUid,
                    sopInstanceUid);

                try
                {
                    await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlErrorCodes.NotFound:
                            throw new DicomInstanceNotFoundException();

                        default:
                            throw new DicomDataStoreException(ex);
                    }
                }
            }
        }
    }
}
