// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.Dicom.SqlServer.Features.Retrieve
{
    internal class SqlInstanceStoreV2 : SqlInstanceStoreV1
    {

        public SqlInstanceStoreV2(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
            : base(sqlConnectionWrapperFactory)
        {
        }

        public override SchemaVersion Version => SchemaVersion.V2;
    }
}
