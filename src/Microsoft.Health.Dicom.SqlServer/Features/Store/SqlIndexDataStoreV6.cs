// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    /// <summary>
    /// Sql IndexDataStore version 6.
    /// </summary>
    internal class SqlIndexDataStoreV6 : SqlIndexDataStoreV5
    {
        public SqlIndexDataStoreV6(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
            : base(sqlConnectionWrapperFactory)
        {
        }

        public override SchemaVersion Version => SchemaVersion.V6;

        public override async Task<long> BeginCreateInstanceIndexAsync(DicomDataset instance, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                var rows = ExtendedQueryTagDataRowsBuilder.Build(instance, queryTags.Where(tag => tag.IsExtendedQueryTag), Version);
                VLatest.AddInstanceV6TableValuedParameters parameters = new VLatest.AddInstanceV6TableValuedParameters(
                    rows.StringRows,
                    rows.LongRows,
                    rows.DoubleRows,
                    rows.DateTimeWithUtcRows,
                    rows.PersonNameRows
                );

                VLatest.AddInstanceV6.PopulateCommand(
                    sqlCommandWrapper,
                    DefaultPartition.Key,
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
                    instance.GetStringDateAsDate(DicomTag.PatientBirthDate),
                    instance.GetSingleValueOrDefault<string>(DicomTag.ManufacturerModelName),
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

        public override async Task DeleteInstanceIndexAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));
            EnsureArg.IsNotNullOrEmpty(sopInstanceUid, nameof(sopInstanceUid));

            await DeleteInstanceAsync(DefaultPartition.Key, studyInstanceUid, seriesInstanceUid, sopInstanceUid, cleanupAfter, cancellationToken);
        }

        public override async Task DeleteSeriesIndexAsync(string studyInstanceUid, string seriesInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));

            await DeleteInstanceAsync(DefaultPartition.Key, studyInstanceUid, seriesInstanceUid, sopInstanceUid: null, cleanupAfter, cancellationToken);
        }

        public override async Task DeleteStudyIndexAsync(string studyInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));

            await DeleteInstanceAsync(DefaultPartition.Key, studyInstanceUid, seriesInstanceUid: null, sopInstanceUid: null, cleanupAfter, cancellationToken);
        }

        public override async Task EndCreateInstanceIndexAsync(
            DicomDataset dicomDataset,
            long watermark,
            IEnumerable<QueryTag> queryTags,
            bool allowExpiredTags = false,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.UpdateInstanceStatusV6.PopulateCommand(
                    sqlCommandWrapper,
                    DefaultPartition.Key,
                    dicomDataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                    dicomDataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
                    dicomDataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty),
                    watermark,
                    (byte)IndexStatus.Created,
                    allowExpiredTags ? null : ExtendedQueryTagDataRowsBuilder.GetMaxTagKey(queryTags));

                try
                {
                    await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    throw ex.Number switch
                    {
                        SqlErrorCodes.NotFound => new InstanceNotFoundException(),
                        SqlErrorCodes.Conflict when ex.State == 10 => new ExtendedQueryTagsOutOfDateException(),
                        _ => new DataStoreException(ex),
                    };
                }
            }
        }

        public override async Task<IEnumerable<VersionedInstanceIdentifier>> RetrieveDeletedInstancesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default)
        {
            var results = new List<VersionedInstanceIdentifier>();

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken, true))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.RetrieveDeletedInstanceV6.PopulateCommand(
                    sqlCommandWrapper,
                    batchSize,
                    maxRetries);

                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    try
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            (int partitionKey, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, long watermark) = reader.ReadRow(
                                VLatest.DeletedInstance.PartitionKey,
                                VLatest.DeletedInstance.StudyInstanceUid,
                                VLatest.DeletedInstance.SeriesInstanceUid,
                                VLatest.DeletedInstance.SopInstanceUid,
                                VLatest.DeletedInstance.Watermark);

                            results.Add(new VersionedInstanceIdentifier(
                                studyInstanceUid,
                                seriesInstanceUid,
                                sopInstanceUid,
                                watermark,
                                partitionKey));
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

        public override async Task DeleteDeletedInstanceAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken, true))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.DeleteDeletedInstanceV6.PopulateCommand(
                    sqlCommandWrapper,
                    DefaultPartition.Key,
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

        public override async Task<int> IncrementDeletedInstanceRetryAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken, true))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.IncrementDeletedInstanceRetryV6.PopulateCommand(
                    sqlCommandWrapper,
                    DefaultPartition.Key,
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

        private async Task DeleteInstanceAsync(int partitionKey, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.DeleteInstanceV6.PopulateCommand(
                    sqlCommandWrapper,
                    cleanupAfter,
                    (byte)IndexStatus.Created,
                    partitionKey,
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

        public override async Task ReindexInstanceAsync(DicomDataset instance, long watermark, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                var rows = ExtendedQueryTagDataRowsBuilder.Build(instance, queryTags, Version);
                VLatest.IndexInstanceV6TableValuedParameters parameters = new VLatest.IndexInstanceV6TableValuedParameters(
                    rows.StringRows,
                    rows.LongRows,
                    rows.DoubleRows,
                    rows.DateTimeWithUtcRows,
                    rows.PersonNameRows);

                VLatest.IndexInstanceV6.PopulateCommand(sqlCommandWrapper, watermark, parameters);

                try
                {
                    await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    throw ex.Number switch
                    {
                        SqlErrorCodes.NotFound => new InstanceNotFoundException(),
                        SqlErrorCodes.Conflict => new PendingInstanceException(),
                        _ => new DataStoreException(ex),
                    };

                }
            }
        }
    }
}
