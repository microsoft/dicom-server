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
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store;

internal class SqlIndexDataStoreV37 : SqlIndexDataStoreV1
{
    public SqlIndexDataStoreV37(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        : base(sqlConnectionWrapperFactory)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V37;

    public override async Task EndCreateInstanceIndexAsync(int partitionKey, DicomDataset dicomDataset, long watermark, IEnumerable<QueryTag> queryTags, FileProperties fileProperties, bool allowExpiredTags, bool hasFrameMetadata = false, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
        EnsureArg.IsNotNull(queryTags, nameof(queryTags));

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.UpdateInstanceStatusV37.PopulateCommand(
                sqlCommandWrapper,
                partitionKey,
                dicomDataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                dicomDataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
                dicomDataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty),
                watermark,
                (byte)IndexStatus.Created,
                allowExpiredTags ? null : ExtendedQueryTagDataRowsBuilder.GetMaxTagKey(queryTags),
                hasFrameMetadata,
                fileProperties?.Path,
                fileProperties?.ETag
            );

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

    public override async Task<long> BeginCreateInstanceIndexAsync(Partition partition, DicomDataset dicomDataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
        EnsureArg.IsNotNull(queryTags, nameof(queryTags));

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            var rows = ExtendedQueryTagDataRowsBuilder.Build(dicomDataset, queryTags.Where(tag => tag.IsExtendedQueryTag), Version);
            VLatest.AddInstanceV6TableValuedParameters parameters = new VLatest.AddInstanceV6TableValuedParameters(
                rows.StringRows,
                rows.LongRows,
                rows.DoubleRows,
                rows.DateTimeWithUtcRows,
                rows.PersonNameRows
            );

            VLatest.AddInstanceV6.PopulateCommand(
                sqlCommandWrapper,
                partition.Key,
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
                dicomDataset.InternalTransferSyntax?.UID.UID,
                parameters);

            try
            {
                return (long)(await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken));
            }
            catch (SqlException ex) when (ex.Number == SqlErrorCodes.Conflict && ex.State == (byte)IndexStatus.Creating)
            {
                throw new PendingInstanceException();
            }
            catch (SqlException ex) when (ex.Number == SqlErrorCodes.Conflict)
            {
                throw new InstanceAlreadyExistsException();
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
                            new Partition(partitionKey, Partition.UnknownName)));
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
                versionedInstanceIdentifier.Partition.Key,
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
                versionedInstanceIdentifier.Partition.Key,
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
        var results = new List<InstanceMetadata>();

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
                        (int partitionKey, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, long watermark, long? originalWatermark) = reader.ReadRow(
                            VLatest.DeletedInstance.PartitionKey,
                            VLatest.DeletedInstance.StudyInstanceUid,
                            VLatest.DeletedInstance.SeriesInstanceUid,
                            VLatest.DeletedInstance.SopInstanceUid,
                            VLatest.DeletedInstance.Watermark,
                            VLatest.DeletedInstance.OriginalWatermark);

                        results.Add(
                        new InstanceMetadata(
                            new VersionedInstanceIdentifier(
                                studyInstanceUid,
                                seriesInstanceUid,
                                sopInstanceUid,
                                watermark,
                                new Partition(partitionKey, Partition.UnknownName)),
                            new InstanceProperties()
                            {
                                OriginalVersion = originalWatermark
                            }));
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

    public override async Task<IReadOnlyList<InstanceMetadata>> BeginUpdateInstancesAsync(Partition partition, string studyInstanceUid, CancellationToken cancellationToken = default)
    {
        var results = new List<InstanceMetadata>();

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.BeginUpdateInstanceV33.PopulateCommand(
                sqlCommandWrapper,
                partition.Key,
                studyInstanceUid);

            try
            {
                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (string rStudyInstanceUid,
                            string rSeriesInstanceUid,
                            string rSopInstanceUid,
                            long watermark,
                            string rTransferSyntaxUid,
                            bool rHasFrameMetadata,
                            long? originalWatermark,
                            long? newWatermark) = reader.ReadRow(
                               VLatest.Instance.StudyInstanceUid,
                               VLatest.Instance.SeriesInstanceUid,
                               VLatest.Instance.SopInstanceUid,
                               VLatest.Instance.Watermark,
                               VLatest.Instance.TransferSyntaxUid,
                               VLatest.Instance.HasFrameMetadata,
                               VLatest.Instance.OriginalWatermark,
                               VLatest.Instance.NewWatermark);

                        results.Add(
                            new InstanceMetadata(
                                new VersionedInstanceIdentifier(
                                    rStudyInstanceUid,
                                    rSeriesInstanceUid,
                                    rSopInstanceUid,
                                    watermark,
                                    partition),
                                new InstanceProperties()
                                {
                                    TransferSyntaxUid = rTransferSyntaxUid,
                                    HasFrameMetadata = rHasFrameMetadata,
                                    OriginalVersion = originalWatermark,
                                    NewVersion = newWatermark
                                }));
                    }
                }
                return results;
            }
            catch (SqlException ex)
            {
                throw ex.Number switch
                {
                    SqlErrorCodes.NotFound => new InstanceNotFoundException(),
                    _ => new DataStoreException(ex),
                };
            }
        }
    }

    public override async Task<IEnumerable<InstanceMetadata>> BeginUpdateInstanceAsync(Partition partition, IReadOnlyCollection<long> versions, CancellationToken cancellationToken = default)
    {
        var results = new List<InstanceMetadata>();

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            List<WatermarkTableTypeRow> versionRows = versions.Select(i => new WatermarkTableTypeRow(i)).ToList();
            VLatest.BeginUpdateInstance.PopulateCommand(
                sqlCommandWrapper,
                partition.Key,
                versionRows);

            try
            {
                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (string rStudyInstanceUid,
                            string rSeriesInstanceUid,
                            string rSopInstanceUid,
                            long watermark,
                            string rTransferSyntaxUid,
                            bool rHasFrameMetadata,
                            long? originalWatermark,
                            long? newWatermark) = reader.ReadRow(
                               VLatest.Instance.StudyInstanceUid,
                               VLatest.Instance.SeriesInstanceUid,
                               VLatest.Instance.SopInstanceUid,
                               VLatest.Instance.Watermark,
                               VLatest.Instance.TransferSyntaxUid,
                               VLatest.Instance.HasFrameMetadata,
                               VLatest.Instance.OriginalWatermark,
                               VLatest.Instance.NewWatermark);

                        results.Add(
                            new InstanceMetadata(
                                new VersionedInstanceIdentifier(
                                    rStudyInstanceUid,
                                    rSeriesInstanceUid,
                                    rSopInstanceUid,
                                    watermark,
                                    partition),
                                new InstanceProperties()
                                {
                                    TransferSyntaxUid = rTransferSyntaxUid,
                                    HasFrameMetadata = rHasFrameMetadata,
                                    OriginalVersion = originalWatermark,
                                    NewVersion = newWatermark
                                }));
                    }
                }
                return results;
            }
            catch (SqlException ex)
            {
                throw ex.Number switch
                {
                    SqlErrorCodes.NotFound => new InstanceNotFoundException(),
                    _ => new DataStoreException(ex),
                };
            }
        }
    }

    public override async Task EndUpdateInstanceAsync(
        int partitionKey,
        string studyInstanceUid,
        DicomDataset dicomDataset,
        IReadOnlyList<InstanceMetadata> instanceMetadataList,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.EndUpdateInstance.PopulateCommand(
                sqlCommandWrapper,
                partitionKey,
                studyInstanceUid,
                dicomDataset.GetFirstValueOrDefault<string>(DicomTag.PatientID),
                dicomDataset.GetFirstValueOrDefault<string>(DicomTag.PatientName),
                dicomDataset.GetStringDateAsDate(DicomTag.PatientBirthDate));

            try
            {
                await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                throw ex.Number switch
                {
                    SqlErrorCodes.NotFound => new StudyNotFoundException(),
                    _ => new DataStoreException(ex),
                };
            }
        }
    }

    protected override async Task<IReadOnlyCollection<VersionedInstanceIdentifier>> DeleteInstanceAsync(Partition partition, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.DeleteInstanceV6.PopulateCommand(
                sqlCommandWrapper,
                cleanupAfter,
                (byte)IndexStatus.Created,
                partition.Key,
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

        return Array.Empty<VersionedInstanceIdentifier>();
    }
}
