// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;

namespace Microsoft.Health.Dicom.SqlServer.Features.Workitem
{
    internal sealed class SqlWorkitemStore : IIndexWorkitemStore
    {
        private readonly VersionedCache<ISqlWorkitemStore> _cache;

        public SqlWorkitemStore(VersionedCache<ISqlWorkitemStore> cache)
            => _cache = EnsureArg.IsNotNull(cache, nameof(cache));

        public async Task<long> BeginAddWorkitemAsync(int partitionKey, DicomDataset dataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default)
        {
            ISqlWorkitemStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.BeginAddWorkitemAsync(partitionKey, dataset, queryTags, cancellationToken);
        }

        public async Task BeginUpdateWorkitemAsync(WorkitemMetadataStoreEntry workitemMetadata, CancellationToken cancellationToken = default)
        {
            var store = await _cache.GetAsync(cancellationToken: cancellationToken);

            await store.BeginUpdateWorkitemAsync(workitemMetadata, cancellationToken);
        }

        public async Task EndUpdateWorkitemAsync(WorkitemMetadataStoreEntry workitemMetadata, DicomDataset dataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default)
        {
            var store = await _cache.GetAsync(cancellationToken: cancellationToken);

            await store.EndUpdateWorkitemAsync(workitemMetadata, dataset, queryTags, cancellationToken);
        }

        public async Task LockWorkitemAsync(WorkitemMetadataStoreEntry workitemMetadata, CancellationToken cancellationToken = default)
        {
            var store = await _cache.GetAsync(cancellationToken: cancellationToken);

            await store.LockWorkitemAsync(workitemMetadata, cancellationToken);
        }

        public async Task UnlockWorkitemAsync(WorkitemMetadataStoreEntry workitemMetadata, CancellationToken cancellationToken = default)
        {
            var store = await _cache.GetAsync(cancellationToken: cancellationToken);

            await store.UnlockWorkitemAsync(workitemMetadata, cancellationToken);
        }

        public async Task DeleteWorkitemAsync(int partitionKey, string workitemUid, CancellationToken cancellationToken = default)
        {
            ISqlWorkitemStore store = await _cache.GetAsync(cancellationToken: cancellationToken);

            await store.DeleteWorkitemAsync(partitionKey, workitemUid, cancellationToken);
        }

        public async Task EndAddWorkitemAsync(int partitionKey, long workitemKey, CancellationToken cancellationToken = default)
        {
            ISqlWorkitemStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            await store.EndAddWorkitemAsync(partitionKey, workitemKey, cancellationToken);
        }

        public async Task<IReadOnlyList<WorkitemQueryTagStoreEntry>> GetWorkitemQueryTagsAsync(CancellationToken cancellationToken = default)
        {
            ISqlWorkitemStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.GetWorkitemQueryTagsAsync(cancellationToken);
        }

        public async Task<WorkitemMetadataStoreEntry> GetWorkitemMetadataAsync(int partitionKey, string workitemUid, CancellationToken cancellationToken = default)
        {
            ISqlWorkitemStore store = await _cache.GetAsync(cancellationToken: cancellationToken);

            return await store.GetWorkitemMetadataAsync(partitionKey, workitemUid, cancellationToken);
        }
    }
}
