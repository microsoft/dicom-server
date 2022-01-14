// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.Dicom.SqlServer.Features.Query
{
    internal class SqlQueryStoreV9 : SqlQueryStoreV6
    {
        // Overriding the version to ensure QueryGenerators receieves V9 schema version for workitem support.
        public override SchemaVersion Version => SchemaVersion.V9;

        public SqlQueryStoreV9(
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
            ILogger<ISqlQueryStore> logger)
            : base(sqlConnectionWrapperFactory, logger)
        {
        }
    }
}
