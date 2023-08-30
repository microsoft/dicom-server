// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;

public class PartitionModel
{
    public PartitionModel(SqlDataReader sqlDataReader)
    {
        EnsureArg.IsNotNull(sqlDataReader, nameof(sqlDataReader));
        Key = sqlDataReader.GetInt32(0);
        Name = sqlDataReader.GetString(1);
        CreatedDate = sqlDataReader.GetDateTime(2);
    }
    public long Key { get; }

    public string Name { get; }

    public DateTimeOffset CreatedDate { get; set; }
}
