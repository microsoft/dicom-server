// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.SqlServer.Features.Schema
{
    public static class SchemaVersionConstants
    {
        public const int Min = (int)SchemaVersion.V2;
        public const int Max = (int)SchemaVersion.V3;
        public const int SupportExtendedQueryTagSchemaVersion = (int)SchemaVersion.V3;
    }
}
