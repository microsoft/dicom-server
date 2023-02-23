// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.SqlServer.Features.Schema;

public static class SchemaVersionConstants
{
    public const int Min = (int)SchemaVersion.V28;
    public const int Max = (int)SchemaVersion.V31;
    public const int SupportExtendedQueryTagSchemaVersion = (int)SchemaVersion.V4;
    public const int SupportDTAndTMInExtendedQueryTagSchemaVersion = (int)SchemaVersion.V5;
    public const int SupportDataPartitionSchemaVersion = (int)SchemaVersion.V6;
    public const int SupportUpsRsSchemaVersion = (int)SchemaVersion.V9;
    public const int SupportUpsRsWatermarkSchemaVersion = (int)SchemaVersion.V11;
}
