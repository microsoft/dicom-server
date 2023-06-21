// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;

public class ChangeFeedRow
{
    public ChangeFeedRow(SqlDataReader sqlDataReader)
    {
        EnsureArg.IsNotNull(sqlDataReader, nameof(sqlDataReader));
        Sequence = sqlDataReader.GetInt64(0);
        Timestamp = sqlDataReader.GetDateTimeOffset(1);
        Action = sqlDataReader.GetByte(2);
        StudyInstanceUid = sqlDataReader.GetString(3);
        SeriesInstanceUid = sqlDataReader.GetString(4);
        SopInstanceUid = sqlDataReader.GetString(5);
        OriginalWatermark = sqlDataReader.GetInt64(6);
        if (!sqlDataReader.IsDBNull(7))
        {
            CurrentWatermark = sqlDataReader.GetInt64(7);
        }
        if (!sqlDataReader.IsDBNull(8))
        {
            FilePath = sqlDataReader.GetString(8);
        }
    }

    public long Sequence { get; }

    public DateTimeOffset Timestamp { get; }

    public int Action { get; }

    public string StudyInstanceUid { get; }

    public string SeriesInstanceUid { get; }

    public string SopInstanceUid { get; }

    public long OriginalWatermark { get; }

    public long? CurrentWatermark { get; }

    public string FilePath { get; }
}
