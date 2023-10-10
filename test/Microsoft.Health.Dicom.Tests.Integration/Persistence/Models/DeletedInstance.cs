// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;

public class DeletedInstance
{
    public DeletedInstance(
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid,
        long watermark,
        DateTimeOffset deletedDateTime,
        int retryCount,
        DateTimeOffset cleanupAfter,
        int? partitionKey,
        long originalWatermark,
        String filePath,
        String eTag)
    {
        StudyInstanceUid = studyInstanceUid;
        SeriesInstanceUid = seriesInstanceUid;
        SopInstanceUid = sopInstanceUid;
        Watermark = watermark;
        DeletedDateTime = deletedDateTime;
        RetryCount = retryCount;
        CleanupAfter = cleanupAfter;
        PartitionKey = partitionKey;
        OriginalWatermark = originalWatermark;
        FilePath = filePath;
        ETag = eTag;
    }

    public DeletedInstance(SqlDataReader sqlDataReader)
    {
        EnsureArg.IsNotNull(sqlDataReader, nameof(sqlDataReader));
        StudyInstanceUid = sqlDataReader.GetString(0);
        SeriesInstanceUid = sqlDataReader.GetString(1);
        SopInstanceUid = sqlDataReader.GetString(2);
        Watermark = sqlDataReader.GetInt64(3);
        DeletedDateTime = sqlDataReader.GetDateTimeOffset(4);
        RetryCount = sqlDataReader.GetInt32(5);
        CleanupAfter = sqlDataReader.GetDateTimeOffset(6);
        PartitionKey = sqlDataReader.IsDBNull(7) ? null : sqlDataReader.GetInt32(7);
        OriginalWatermark = sqlDataReader.IsDBNull(8) ? null : sqlDataReader.GetInt64(8);
        FilePath = sqlDataReader.IsDBNull(9) ? null : sqlDataReader.GetString(9);
        ETag = sqlDataReader.IsDBNull(10) ? null : sqlDataReader.GetString(10);
    }

    public string StudyInstanceUid { get; }

    public string SeriesInstanceUid { get; }

    public string SopInstanceUid { get; }

    public long Watermark { get; }

    public DateTimeOffset DeletedDateTime { get; }

    public int RetryCount { get; }

    public DateTimeOffset CleanupAfter { get; }

    public int? PartitionKey { get; }

    public String ETag { get; }

    public String FilePath { get; }

    public object OriginalWatermark { get; }
}
