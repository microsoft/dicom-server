// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;

public class FileProperty
{
    public FileProperty(
        long instanceKey,
        long watermark,
        string filePath,
        string eTag)
    {
        InstanceKey = instanceKey;
        Watermark = watermark;
        FilePath = filePath;
        ETag = eTag;
    }

    public FileProperty(SqlDataReader sqlDataReader)
    {
        EnsureArg.IsNotNull(sqlDataReader, nameof(sqlDataReader));
        InstanceKey = sqlDataReader.GetInt64(0);
        Watermark = sqlDataReader.GetInt64(1);
        FilePath = sqlDataReader.GetString(2);
        ETag = sqlDataReader.GetString(3);
    }

    public long InstanceKey { get; private set; }

    public long Watermark { get; private set; }

    public string FilePath { get; }

    public string ETag { get; }
}
