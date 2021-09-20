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
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    /// <summary>
    /// Sql IndexDataStore version 5.
    /// </summary>
    internal class SqlIndexDataStoreV5 : SqlIndexDataStoreV4
    {
        public SqlIndexDataStoreV5(
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
            : base(sqlConnectionWrapperFactory)
        {
        }

        public override SchemaVersion Version => SchemaVersion.V5;

        public override async Task<long> BeginCreateInstanceIndexAsync(DicomDataset instance, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.BeginAddInstance.PopulateCommand(
                    sqlCommandWrapper,
                    partitionId: null,
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
                    instance.GetSingleValueOrDefault<string>(DicomTag.ManufacturerModelName));

                try
                {
                    return (long)(await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken));
                }
                catch (SqlException ex)
                {
                    throw ex.Number switch
                    {
                        SqlErrorCodes.Conflict => ex.State switch
                        {
                            (byte)IndexStatus.Creating => new PendingInstanceException(),
                            (byte)IndexStatus.Created => new InstanceAlreadyExistsException(),
                            _ => new ExtendedQueryTagsOutOfDateException(),
                        },
                        _ => new DataStoreException(ex),
                    };
                }
            }
        }

        public override async Task EndCreateInstanceIndexAsync(
            DicomDataset instance,
            long watermark,
            IEnumerable<QueryTag> queryTags,
            bool allowExpiredTags = false,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            var rows = ExtendedQueryTagDataRowsBuilder.Build(instance, queryTags);
            VLatest.EndAddInstanceTableValuedParameters parameters = new VLatest.EndAddInstanceTableValuedParameters(
                rows.StringRows,
                rows.LongRows,
                rows.DoubleRows,
                rows.DateTimeRows,
                rows.PersonNameRows);

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.EndAddInstance.PopulateCommand(
                    sqlCommandWrapper,
                    partitionId: null,
                    instance.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                    instance.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
                    instance.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty),
                    watermark,
                    allowExpiredTags ? null : rows.MaxTagKey, // MaxTagKey helps track if any new tags have been added mid-operation
                    parameters);

                try
                {
                    await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    throw ex.Number switch
                    {
                        SqlErrorCodes.Conflict => new ExtendedQueryTagsOutOfDateException(),
                        SqlErrorCodes.NotFound => new InstanceNotFoundException(),
                        _ => new DataStoreException(ex),
                    };
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
                var rows = ExtendedQueryTagDataRowsBuilder.Build(instance, queryTags);
                VLatest.IndexInstanceTableValuedParameters parameters = new VLatest.IndexInstanceTableValuedParameters(
                    rows.StringRows,
                    rows.LongRows,
                    rows.DoubleRows,
                    rows.DateTimeRows,
                    rows.PersonNameRows);

                VLatest.IndexInstance.PopulateCommand(sqlCommandWrapper, watermark, parameters);

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

        public override async Task DeleteInstanceIndexAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));
            EnsureArg.IsNotNullOrEmpty(sopInstanceUid, nameof(sopInstanceUid));

            await DeleteInstanceAsync(partitionId: null, studyInstanceUid, seriesInstanceUid, sopInstanceUid, cleanupAfter, cancellationToken);
        }

        public override async Task DeleteSeriesIndexAsync(string studyInstanceUid, string seriesInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));

            await DeleteInstanceAsync(partitionId: null, studyInstanceUid, seriesInstanceUid, sopInstanceUid: null, cleanupAfter, cancellationToken);
        }

        public override async Task DeleteStudyIndexAsync(string studyInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));

            await DeleteInstanceAsync(partitionId: null, studyInstanceUid, seriesInstanceUid: null, sopInstanceUid: null, cleanupAfter, cancellationToken);
        }

        public override async Task<IEnumerable<VersionedInstanceIdentifier>> RetrieveDeletedInstancesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default)
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
                            (string partitionId, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, long watermark) = reader.ReadRow(
                                VLatest.DeletedInstance.PartitionId,
                                VLatest.DeletedInstance.StudyInstanceUid,
                                VLatest.DeletedInstance.SeriesInstanceUid,
                                VLatest.DeletedInstance.SopInstanceUid,
                                VLatest.DeletedInstance.Watermark);

                            results.Add(new VersionedInstanceIdentifier(
                                studyInstanceUid,
                                seriesInstanceUid,
                                sopInstanceUid,
                                watermark,
                                partitionId));
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
                VLatest.DeleteDeletedInstance.PopulateCommand(
                    sqlCommandWrapper,
                    versionedInstanceIdentifier.PartitionId,
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
                VLatest.IncrementDeletedInstanceRetry.PopulateCommand(
                    sqlCommandWrapper,
                    versionedInstanceIdentifier.PartitionId,
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

        public override async Task CheckIfInstancesExistAsync(CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken, true))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.CheckIfInstancesExist.PopulateCommand(sqlCommandWrapper);

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

        private async Task DeleteInstanceAsync(string partitionId, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.DeleteInstance.PopulateCommand(
                    sqlCommandWrapper,
                    cleanupAfter,
                    (byte)IndexStatus.Created,
                    partitionId,
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
    }
}
