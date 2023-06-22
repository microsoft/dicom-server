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
using EnsureThat;
using FellowOakDicom;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store;

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

    public override async Task<long> BeginCreateInstanceIndexAsync(int partitionKey, DicomDataset dicomDataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
        EnsureArg.IsNotNull(queryTags, nameof(queryTags));

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            var rows = ExtendedQueryTagDataRowsBuilder.Build(dicomDataset, queryTags.Where(tag => tag.IsExtendedQueryTag), Version);
            V6.AddInstanceV6TableValuedParameters parameters = new V6.AddInstanceV6TableValuedParameters(
                rows.StringRows,
                rows.LongRows,
                rows.DoubleRows,
                rows.DateTimeWithUtcRows,
                rows.PersonNameRows
            );

            V6.AddInstanceV6.PopulateCommand(
                sqlCommandWrapper,
                partitionKey,
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
                dicomDataset.GetStringDateAsDate(DicomTag.PatientBirthDate),
                dicomDataset.GetFirstValueOrDefault<string>(DicomTag.ManufacturerModelName),
                (byte)IndexStatus.Creating,
                parameters);

            try
            {
                return (long)await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
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

    public override async Task DeleteInstanceIndexAsync(int partitionKey, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
        EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));
        EnsureArg.IsNotNullOrEmpty(sopInstanceUid, nameof(sopInstanceUid));

        await DeleteInstanceAsync(partitionKey, studyInstanceUid, seriesInstanceUid, sopInstanceUid, cleanupAfter, cancellationToken);
    }

    public override async Task DeleteSeriesIndexAsync(int partitionKey, string studyInstanceUid, string seriesInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
        EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));

        await DeleteInstanceAsync(partitionKey, studyInstanceUid, seriesInstanceUid, sopInstanceUid: null, cleanupAfter, cancellationToken);
    }

    public override async Task DeleteStudyIndexAsync(int partitionKey, string studyInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));

        await DeleteInstanceAsync(partitionKey, studyInstanceUid, seriesInstanceUid: null, sopInstanceUid: null, cleanupAfter, cancellationToken);
    }

    public override async Task EndCreateInstanceIndexAsync(
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
        EnsureArg.IsNotNull(queryTags, nameof(queryTags));

        if (fileProperties != null)
        {
            // if we are passing in fileProperties, it means we're using a new binary, but with an old schema
            throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
        }

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            V22.UpdateInstanceStatusV6.PopulateCommand(
                sqlCommandWrapper,
                partitionKey,
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
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
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
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.DeleteDeletedInstanceV6.PopulateCommand(
                sqlCommandWrapper,
                versionedInstanceIdentifier.PartitionKey,
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
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.IncrementDeletedInstanceRetryV6.PopulateCommand(
                sqlCommandWrapper,
                versionedInstanceIdentifier.PartitionKey,
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
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
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

    public override async Task ReindexInstanceAsync(DicomDataset dicomDataset, long watermark, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
        EnsureArg.IsNotNull(queryTags, nameof(queryTags));

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            var rows = ExtendedQueryTagDataRowsBuilder.Build(dicomDataset, queryTags, Version);
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

    public override async Task<IReadOnlyList<InstanceMetadata>> RetrieveDeletedInstancesWithPropertiesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default)
    {
        IEnumerable<VersionedInstanceIdentifier> results = await RetrieveDeletedInstancesAsync(batchSize, maxRetries, cancellationToken);
        return results.Select(x => new InstanceMetadata(x, new InstanceProperties())).ToList();
    }
}
