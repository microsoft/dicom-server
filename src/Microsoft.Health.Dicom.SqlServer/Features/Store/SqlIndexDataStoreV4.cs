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
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    /// <summary>
    /// Sql IndexDataStore version 4.
    /// </summary>
    internal class SqlIndexDataStoreV4 : SqlIndexDataStoreV3
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionFactoryWrapper;

        public SqlIndexDataStoreV4(
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
            : base(sqlConnectionWrapperFactory)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            _sqlConnectionFactoryWrapper = sqlConnectionWrapperFactory;
        }

        public override SchemaVersion Version => SchemaVersion.V4;

        public override async Task<long> CreateInstanceIndexAsync(DicomDataset instance, IEnumerable<QueryTag> queryTags, string partitionId, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                // Build parameter for extended query tag.
                VLatest.AddInstanceTableValuedParameters parameters = AddInstanceTableValuedParametersBuilder.Build(
                    instance,
                    queryTags.Where(tag => tag.IsExtendedQueryTag));

                VLatest.AddInstance.PopulateCommand(
                sqlCommandWrapper,
                instance.GetString(DicomTag.StudyInstanceUID),
                instance.GetString(DicomTag.SeriesInstanceUID),
                instance.GetString(DicomTag.SOPInstanceUID),
                partitionId,
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

        public override async Task UpdateInstanceIndexStatusAsync(
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
                VLatest.UpdateInstanceStatus.PopulateCommand(
                    sqlCommandWrapper,
                    versionedInstanceIdentifier.StudyInstanceUid,
                    versionedInstanceIdentifier.SeriesInstanceUid,
                    versionedInstanceIdentifier.SopInstanceUid,
                    versionedInstanceIdentifier.PartitionId,
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

        public override async Task DeleteDeletedInstanceAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapperAsync(cancellationToken, true))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.DeleteDeletedInstance.PopulateCommand(
                    sqlCommandWrapper,
                    versionedInstanceIdentifier.StudyInstanceUid,
                    versionedInstanceIdentifier.SeriesInstanceUid,
                    versionedInstanceIdentifier.SopInstanceUid,
                    versionedInstanceIdentifier.PartitionId,
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
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapperAsync(cancellationToken, true))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.IncrementDeletedInstanceRetry.PopulateCommand(
                    sqlCommandWrapper,
                    versionedInstanceIdentifier.StudyInstanceUid,
                    versionedInstanceIdentifier.SeriesInstanceUid,
                    versionedInstanceIdentifier.SopInstanceUid,
                    versionedInstanceIdentifier.PartitionId,
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

        public override async Task<IEnumerable<VersionedInstanceIdentifier>> RetrieveDeletedInstancesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default)
        {
            var results = new List<VersionedInstanceIdentifier>();

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapperAsync(cancellationToken, true))
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
                            (string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string partitionId, long watermark) = reader.ReadRow(
                                VLatest.DeletedInstance.StudyInstanceUid,
                                VLatest.DeletedInstance.SeriesInstanceUid,
                                VLatest.DeletedInstance.SopInstanceUid,
                                VLatest.DeletedInstance.PartitionId,
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

        protected override async Task DeleteInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string partitionId, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.DeleteInstance.PopulateCommand(
                    sqlCommandWrapper,
                    cleanupAfter,
                    (byte)IndexStatus.Created,
                    studyInstanceUid,
                    seriesInstanceUid,
                    sopInstanceUid,
                    partitionId);

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
