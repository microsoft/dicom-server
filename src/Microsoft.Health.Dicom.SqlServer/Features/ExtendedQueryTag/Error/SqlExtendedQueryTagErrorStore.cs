// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag.Error
{
    internal sealed class SqlExtendedQueryTagErrorStore : IExtendedQueryTagErrorStore
    {
        private readonly VersionedCache<ISqlExtendedQueryTagErrorStore> _cache;

        public SqlExtendedQueryTagErrorStore(VersionedCache<ISqlExtendedQueryTagErrorStore> cache)
            => _cache = EnsureArg.IsNotNull(cache, nameof(cache));

        public async Task<int> AddExtendedQueryTagErrorAsync(
            int tagKey,
            short errorCode,
            long watermark,
            DateTime createdTime,
            CancellationToken cancellationToken = default)
        {
            ISqlExtendedQueryTagErrorStore store = await _cache.GetAsync(cancellationToken);
            return await store.AddExtendedQueryTagErrorAsync(
                tagKey,
                errorCode,
                watermark,
                createdTime,
                cancellationToken);
        }

        public async Task<IReadOnlyList<ExtendedQueryTagError>> GetExtendedQueryTagErrorsAsync(string tagPath, CancellationToken cancellationToken = default)
        {
            ISqlExtendedQueryTagErrorStore store = await _cache.GetAsync(cancellationToken);
            return await store.GetExtendedQueryTagErrorsAsync(tagPath, cancellationToken);
        }
    }
}
