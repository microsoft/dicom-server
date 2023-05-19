// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store;

/// <summary>
/// Sql IndexDataStore version 36.
/// </summary>
internal class SqlIndexDataStoreV36 : SqlIndexDataStoreV35
{
    public SqlIndexDataStoreV36(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        : base(sqlConnectionWrapperFactory)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V36;

    public override async Task EndCreateInstanceIndexAsync(
        int partitionKey,
        DicomDataset dicomDataset,
        long watermark,
        IEnumerable<QueryTag> queryTags,
        bool allowExpiredTags = false,
        bool hasFrameMetadata = false,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
        EnsureArg.IsNotNull(queryTags, nameof(queryTags));

        using SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
        using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();
        VLatest.UpdateInstanceStatusV36.PopulateCommand(
            sqlCommandWrapper,
            partitionKey,
            dicomDataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
            dicomDataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
            dicomDataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty),
            watermark,
            (byte)IndexStatus.Created,
            allowExpiredTags ? null : ExtendedQueryTagDataRowsBuilder.GetMaxTagKey(queryTags),
            hasFrameMetadata);

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

    public override async Task EndUpdateInstanceAsync(
        int partitionKey,
        string studyInstanceUid,
        DicomDataset dicomDataset,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

        using SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
        using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();
        VLatest.EndUpdateInstanceV36.PopulateCommand(
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

    protected override async Task DeleteInstanceAsync(int partitionKey, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
    {
        using SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
        using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();
        VLatest.DeleteInstanceV36.PopulateCommand(
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
