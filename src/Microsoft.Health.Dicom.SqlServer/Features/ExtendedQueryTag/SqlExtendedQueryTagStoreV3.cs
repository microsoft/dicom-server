// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag
{
    internal class SqlExtendedQueryTagStoreV3 : SqlExtendedQueryTagStoreV2
    {
        public SqlExtendedQueryTagStoreV3(
           SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
           ILogger<SqlExtendedQueryTagStoreV3> logger)
            : base(sqlConnectionWrapperFactory, logger)
        {
        }

        public override SchemaVersion Version => SchemaVersion.V3;
        public override async Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> GetExtendedQueryTagsAsync(int limit, int offset, CancellationToken cancellationToken = default)
        {
            var tags = await GetAllExtendedQueryTagsAsync(cancellationToken);
            tags.Sort((entry1, entry2) => entry1.Key - entry2.Key);
            if (offset < 0 || offset >= tags.Count || limit <= 0)
            {
                return Array.Empty<ExtendedQueryTagStoreEntry>();
            }

            return tags.GetRange(offset, Math.Min(limit, tags.Count - offset));
        }
    }
}
