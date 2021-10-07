// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag.Error
{
    internal class SqlExtendedQueryTagErrorStoreV6 : SqlExtendedQueryTagErrorStoreV5
    {
        public SqlExtendedQueryTagErrorStoreV6(
           SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
           ILogger<SqlExtendedQueryTagErrorStoreV6> logger)
            : base(sqlConnectionWrapperFactory, logger)
        {
        }

        public override SchemaVersion Version => SchemaVersion.V6;
    }
}
