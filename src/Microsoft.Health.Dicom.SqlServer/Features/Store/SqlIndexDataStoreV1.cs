// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    /// <summary>
    /// Sql IndexDataStore version 1.
    /// </summary>
    internal class SqlIndexDataStoreV1 : ISqlIndexDataStore
    {
        public SqlIndexDataStoreV1(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        {
            SqlConnectionWrapperFactory = EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
        }

        protected SqlConnectionWrapperFactory SqlConnectionWrapperFactory { get; }

        public virtual SchemaVersion Version => SchemaVersion.V1;

        public virtual async Task<long> BeginCreateInstanceIndexAsync(DicomDataset instance, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                V1.AddInstance.PopulateCommand(
                    sqlCommandWrapper,
                    instance.GetString(DicomTag.StudyInstanceUID),
                    instance.GetString(DicomTag.SeriesInstanceUID),
                    instance.GetString(DicomTag.SOPInstanceUID),
                    instance.GetSingleValueOrDefault<string>(DicomTag.PatientID),
                    instance.GetSingleValueOrDefault<string>(DicomTag.PatientName),
                    instance.GetSingleValueOrDefault<string>(DicomTag.ReferringPhysicianName),
                    instance.GetStringDateAsDate(DicomTag.StudyDate),
                    instance.GetSingleValueOrDefault<string>(DicomTag.StudyDescription),
                    instance.GetSingleValueOrDefault<string>(DicomTag.AccessionNumber),
                    instance.GetSingleValueOrDefault<string>(DicomTag.Modality),
                    instance.GetStringDateAsDate(DicomTag.PerformedProcedureStepStartDate),
                    (byte)IndexStatus.Creating);

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

        public virtual async Task DeleteInstanceIndexAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));
            EnsureArg.IsNotNullOrEmpty(sopInstanceUid, nameof(sopInstanceUid));

            await DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, cleanupAfter, cancellationToken);
        }

        public virtual async Task DeleteSeriesIndexAsync(string studyInstanceUid, string seriesInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));

            await DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid: null, cleanupAfter, cancellationToken);
        }

        public virtual async Task DeleteStudyIndexAsync(string studyInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));

            await DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid: null, sopInstanceUid: null, cleanupAfter, cancellationToken);
        }

        public virtual async Task EndCreateInstanceIndexAsync(
            DicomDataset dicomDataset,
            long watermark,
            IEnumerable<QueryTag> queryTags,
            bool allowExpiredTags = false,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                V1.UpdateInstanceStatus.PopulateCommand(
                    sqlCommandWrapper,
                    dicomDataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                    dicomDataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
                    dicomDataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty),
                    watermark,
                    (byte)IndexStatus.Created);

                try
                {
                    await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlErrorCodes.NotFound:
                            throw new InstanceNotFoundException();

                        default:
                            throw new DataStoreException(ex);
                    }
                }
            }
        }

        public virtual async Task<IEnumerable<VersionedInstanceIdentifier>> RetrieveDeletedInstancesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default)
        {
            var results = new List<VersionedInstanceIdentifier>();

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken, true))
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

                            results.Add(new VersionedInstanceIdentifier(
                                studyInstanceUid,
                                seriesInstanceUid,
                                sopInstanceUid,
                                watermark));
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new DataStoreException(ex);
                    }
                }
            }

            return results;
        }

        public virtual async Task DeleteDeletedInstanceAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken, true))
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
                    throw new DataStoreException(ex);
                }
            }
        }

        public virtual async Task<int> IncrementDeletedInstanceRetryAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken, true))
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
                    throw new DataStoreException(ex);
                }
            }
        }

        private async Task DeleteInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.DeleteInstance.PopulateCommand(
                    sqlCommandWrapper,
                    cleanupAfter,
                    (byte)IndexStatus.Created,
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
                            if (!string.IsNullOrEmpty(sopInstanceUid))
                            {
                                throw new InstanceNotFoundException();
                            }

                            if (!string.IsNullOrEmpty(seriesInstanceUid))
                            {
                                throw new SeriesNotFoundException();
                            }

                            throw new StudyNotFoundException();

                        default:
                            throw new DataStoreException(ex);
                    }
                }
            }
        }

        public async Task<int> RetrieveNumExhaustedDeletedInstanceAttemptsAsync(int maxNumberOfRetries, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                sqlCommandWrapper.CommandText = @$"
                        SELECT COUNT(*)
                        FROM {VLatest.DeletedInstance.TableName}
                        WHERE {VLatest.DeletedInstance.RetryCount} >= @maxNumberOfRetries";

                sqlCommandWrapper.Parameters.AddWithValue("@maxNumberOfRetries", maxNumberOfRetries);


                try
                {
                    using (SqlDataReader sqlDataReader = await sqlCommandWrapper.ExecuteReaderAsync(cancellationToken))
                    {
                        await sqlDataReader.ReadAsync(cancellationToken);
                        return (int)sqlDataReader[0];
                    }
                }
                catch (SqlException ex)
                {
                    throw new DataStoreException(ex);
                }
            }
        }

        public async Task<DateTimeOffset> GetOldestDeletedAsync(CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                sqlCommandWrapper.CommandText = @$"
                        SELECT MIN({VLatest.DeletedInstance.DeletedDateTime})
                        FROM {VLatest.DeletedInstance.TableName}";
                try
                {
                    using (SqlDataReader sqlDataReader = await sqlCommandWrapper.ExecuteReaderAsync(cancellationToken))
                    {
                        await sqlDataReader.ReadAsync(cancellationToken);

                        if (sqlDataReader.IsDBNull(0))
                        {
                            return DateTimeOffset.UtcNow;
                        }

                        return (DateTimeOffset)sqlDataReader[0];
                    }
                }
                catch (SqlException ex)
                {
                    throw new DataStoreException(ex);
                }
            }
        }

        public virtual Task ReindexInstanceAsync(DicomDataset dicomDataset, long watermark, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default)
        {
            throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
        }
    }
}
