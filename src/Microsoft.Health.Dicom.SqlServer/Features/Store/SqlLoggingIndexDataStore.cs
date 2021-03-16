// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    internal class SqlLoggingIndexDataStore : LoggingIndexDataStore, ISqlIndexDataStore
    {
        public SqlLoggingIndexDataStore(ISqlIndexDataStore indexDataStore, ILogger<SqlLoggingIndexDataStore> logger)
            : base(indexDataStore, logger)
        {
        }

        public SchemaVersion Version => ((ISqlIndexDataStore)IndexDataStore).Version;
    }
}
