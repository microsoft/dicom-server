// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.Dicom.SqlServer.Features.Query
{
    internal class SqlQueryStoreV2 : SqlQueryStore
    {
        public override SchemaVersion Version => SchemaVersion.V2;

        public SqlQueryStoreV2(
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
            ILogger<SqlQueryStore> logger)
            : base(sqlConnectionWrapperFactory, logger)
        {
        }
    }
}
