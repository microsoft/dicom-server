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
        private readonly SqlConnectionWrapperFactory _sqlConnectionFactoryWrapper;

        public SqlIndexDataStoreV1(
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            _sqlConnectionFactoryWrapper = sqlConnectionWrapperFactory;
        }

        public virtual SchemaVersion Version => SchemaVersion.V1;

        public virtual async Task<long> CreateInstanceIndexAsync(DicomDataset instance, IEnumerable<QueryTag> queryTags, string partitionId = null, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapperAsync(cancellationToken))
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

        public virtual async Task DeleteInstanceIndexAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string partitionId, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));
            EnsureArg.IsNotNullOrEmpty(sopInstanceUid, nameof(sopInstanceUid));

            await DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, partitionId, cleanupAfter, cancellationToken);
        }

        public virtual async Task DeleteSeriesIndexAsync(string studyInstanceUid, string seriesInstanceUid, string partitionId, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));

            await DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid: null, partitionId, cleanupAfter, cancellationToken);
        }

        public virtual async Task DeleteStudyIndexAsync(string studyInstanceUid, string partitionId, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));

            await DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid: null, sopInstanceUid: null, partitionId, cleanupAfter, cancellationToken);
        }

        public virtual async Task UpdateInstanceIndexStatusAsync(
            VersionedInstanceIdentifier versionedInstanceIdentifier,
            IndexStatus status,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));
            EnsureArg.IsTrue(Enum.IsDefined(typeof(IndexStatus), status));
            EnsureArg.IsTrue((int)status < byte.MaxValue);

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                V3.UpdateInstanceStatus.PopulateCommand(
                    sqlCommandWrapper,
                    versionedInstanceIdentifier.StudyInstanceUid,
                    versionedInstanceIdentifier.SeriesInstanceUid,
                    versionedInstanceIdentifier.SopInstanceUid,
                    versionedInstanceIdentifier.Version,
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

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapperAsync(cancellationToken, true))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                V3.RetrieveDeletedInstance.PopulateCommand(
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
                                V3.DeletedInstance.StudyInstanceUid,
                                V3.DeletedInstance.SeriesInstanceUid,
                                V3.DeletedInstance.SopInstanceUid,
                                V3.DeletedInstance.Watermark);

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
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapperAsync(cancellationToken, true))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                V3.DeleteDeletedInstance.PopulateCommand(
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
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapperAsync(cancellationToken, true))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                V3.IncrementDeletedInstanceRetry.PopulateCommand(
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

        protected virtual async Task DeleteInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string partitionId, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                V3.DeleteInstance.PopulateCommand(
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
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapperAsync(cancellationToken))
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
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapperAsync(cancellationToken))
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
    }
}
