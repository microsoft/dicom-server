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

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag
{
    internal sealed class SqlExtendedQueryTagStore : IExtendedQueryTagStore
    {
        private readonly VersionedCache<ISqlExtendedQueryTagStore> _cache;

        public SqlExtendedQueryTagStore(VersionedCache<ISqlExtendedQueryTagStore> cache)
            => _cache = EnsureArg.IsNotNull(cache, nameof(cache));

        public async Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> AddExtendedQueryTagsAsync(IEnumerable<AddExtendedQueryTagEntry> extendedQueryTagEntries, int maxAllowedCount, bool ready = false, CancellationToken cancellationToken = default)
        {
            ISqlExtendedQueryTagStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.AddExtendedQueryTagsAsync(extendedQueryTagEntries, maxAllowedCount, ready, cancellationToken);
        }

        public async Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> AssignReindexingOperationAsync(IReadOnlyList<int> queryTagKeys, Guid operationId, bool returnIfCompleted = false, CancellationToken cancellationToken = default)
        {
            ISqlExtendedQueryTagStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.AssignReindexingOperationAsync(queryTagKeys, operationId, returnIfCompleted, cancellationToken);
        }

        public async Task<IReadOnlyList<int>> CompleteReindexingAsync(IReadOnlyList<int> queryTagKeys, CancellationToken cancellationToken = default)
        {
            ISqlExtendedQueryTagStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.CompleteReindexingAsync(queryTagKeys, cancellationToken);
        }

        public async Task DeleteExtendedQueryTagAsync(string tagPath, string vr, CancellationToken cancellationToken = default)
        {
            ISqlExtendedQueryTagStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            await store.DeleteExtendedQueryTagAsync(tagPath, vr, cancellationToken);
        }

        public async Task<ExtendedQueryTagStoreJoinEntry> GetExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken = default)
        {
            ISqlExtendedQueryTagStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.GetExtendedQueryTagAsync(tagPath, cancellationToken);
        }

        public async Task<IReadOnlyList<ExtendedQueryTagStoreJoinEntry>> GetExtendedQueryTagsAsync(int limit, int offset, CancellationToken cancellationToken = default)
        {
            ISqlExtendedQueryTagStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.GetExtendedQueryTagsAsync(limit, offset, cancellationToken);
        }

        public async Task<IReadOnlyList<ExtendedQueryTagStoreJoinEntry>> GetExtendedQueryTagsAsync(IReadOnlyList<int> queryTagKeys, CancellationToken cancellationToken = default)
        {
            ISqlExtendedQueryTagStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.GetExtendedQueryTagsAsync(queryTagKeys, cancellationToken);
        }

        public async Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> GetExtendedQueryTagsAsync(Guid operationId, CancellationToken cancellationToken = default)
        {
            ISqlExtendedQueryTagStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.GetExtendedQueryTagsAsync(operationId, cancellationToken);
        }

        ///<inheritdoc/>
        public async Task<ExtendedQueryTagStoreJoinEntry> UpdateQueryStatusAsync(string tagPath, QueryStatus queryStatus, CancellationToken cancellationToken = default)
        {
            ISqlExtendedQueryTagStore store = await _cache.GetAsync(cancellationToken);
            return await store.UpdateQueryStatusAsync(tagPath, queryStatus, cancellationToken);
        }
    }
}
