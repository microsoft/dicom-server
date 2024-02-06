// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store;

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

    public virtual async Task<long> BeginCreateInstanceIndexAsync(Partition partition, DicomDataset dicomDataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
        EnsureArg.IsNotNull(queryTags, nameof(queryTags));

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            V1.AddInstance.PopulateCommand(
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

    public virtual Task<IReadOnlyCollection<VersionedInstanceIdentifier>> DeleteInstanceIndexAsync(Partition partition, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
        EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));
        EnsureArg.IsNotNullOrEmpty(sopInstanceUid, nameof(sopInstanceUid));

        return DeleteInstanceAsync(partition, studyInstanceUid, seriesInstanceUid, sopInstanceUid, cleanupAfter, cancellationToken);
    }

    public virtual Task<IReadOnlyCollection<VersionedInstanceIdentifier>> DeleteSeriesIndexAsync(Partition partition, string studyInstanceUid, string seriesInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
        EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));

        return DeleteInstanceAsync(partition, studyInstanceUid, seriesInstanceUid, sopInstanceUid: null, cleanupAfter, cancellationToken);
    }

    public virtual Task<IReadOnlyCollection<VersionedInstanceIdentifier>> DeleteStudyIndexAsync(Partition partition, string studyInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));

        return DeleteInstanceAsync(partition, studyInstanceUid, seriesInstanceUid: null, sopInstanceUid: null, cleanupAfter, cancellationToken);
    }

    public virtual async Task EndCreateInstanceIndexAsync(
        int partitionKey,
        DicomDataset dicomDataset,
        long watermark,
        IEnumerable<QueryTag> queryTags,
        FileProperties fileProperties,
        bool allowExpiredTags,
        bool hasFrameMetadata,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

        if (fileProperties != null)
        {
            // if we are passing in fileProperties, it means we're using a new binary, but with an old schema
            throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
        }

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
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
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
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
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
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
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
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

    protected virtual async Task<IReadOnlyCollection<VersionedInstanceIdentifier>> DeleteInstanceAsync(Partition partition, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            V8.DeleteInstance.PopulateCommand(
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

            return Array.Empty<VersionedInstanceIdentifier>();
        }
    }

    public async Task<int> RetrieveNumExhaustedDeletedInstanceAttemptsAsync(int maxNumberOfRetries, CancellationToken cancellationToken = default)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
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
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            sqlCommandWrapper.CommandText = @$"
                        SELECT MIN({VLatest.DeletedInstance.DeletedDateTime})
                        FROM {VLatest.DeletedInstance.TableName}";
            try
            {
                using (SqlDataReader sqlDataReader = await sqlCommandWrapper.ExecuteReaderAsync(cancellationToken))
                {
                    await sqlDataReader.ReadAsync(cancellationToken);

                    if (await sqlDataReader.IsDBNullAsync(0, cancellationToken))
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

    public virtual Task<IEnumerable<InstanceMetadata>> BeginUpdateInstanceAsync(Partition partition, IReadOnlyCollection<long> versions, CancellationToken cancellationToken = default)
    {
        throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
    }

    public virtual Task<IReadOnlyList<InstanceMetadata>> BeginUpdateInstancesAsync(Partition partition, string studyInstanceUid, CancellationToken cancellationToken = default)
    {
        throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
    }

    public virtual Task EndUpdateInstanceAsync(int partitionKey, string studyInstanceUid, DicomDataset dicomDataset, IReadOnlyList<InstanceMetadata> instanceMetadataList, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default)
    {
        throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
    }

    public virtual Task<IReadOnlyList<InstanceMetadata>> RetrieveDeletedInstancesWithPropertiesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default)
    {
        throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
    }

    public virtual Task UpdateFrameDataAsync(int partitionKey, IEnumerable<long> versions, bool hasFrameMetadata, CancellationToken cancellationToken = default)
    {
        throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
    }

    public virtual Task UpdateFilePropertiesContentLengthAsync(
        IReadOnlyDictionary<long, FileProperties> filePropertiesByWatermark,
        CancellationToken cancellationToken = default)
    {
        throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
    }

    public virtual Task<IndexedFileProperties> GetIndexedFileMetricsAsync(CancellationToken cancellationToken = default)
    {
        throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
    }
}
