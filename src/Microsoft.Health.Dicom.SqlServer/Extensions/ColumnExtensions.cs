// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Dicom.SqlServer.Extensions;

internal static class ColumnExtensions
{
    // TODO: Support additional types

    public static NullableUniqueIdentifierColumn AsNullable(this UniqueIdentifierColumn column)
        => new NullableUniqueIdentifierColumn(column.Metadata.Name);

    public static NullableNVarCharColumn AsNullable(this NVarCharColumn column)
        => new NullableNVarCharColumn(column.Metadata.Name, (int)column.Metadata.MaxLength);
}
