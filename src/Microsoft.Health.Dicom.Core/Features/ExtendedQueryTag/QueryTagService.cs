// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Features.Common;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class QueryTagService : IQueryTagService, IDisposable
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly AsyncCache<List<QueryTag>> _queryTagCache;
        private readonly bool _enableExtendedQueryTags;
        private bool _disposed;

        public QueryTagService(IExtendedQueryTagStore extendedQueryTagStore, IOptions<FeatureConfiguration> featureConfiguration)
        {
            _extendedQueryTagStore = EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            _queryTagCache = new AsyncCache<List<QueryTag>>(ResolveQueryTagsAsync);
            _enableExtendedQueryTags = EnsureArg.IsNotNull(featureConfiguration?.Value.EnableExtendedQueryTags, nameof(featureConfiguration)).GetValueOrDefault();
        }

        public static IReadOnlyList<QueryTag> CoreQueryTags { get; } = QueryLimit.CoreTags.Select(tag => new QueryTag(tag)).ToList();

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _queryTagCache.Dispose();
                }

                _disposed = true;
            }
        }

        public async Task<IReadOnlyCollection<QueryTag>> GetQueryTagsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(QueryTagService));
            }

            return _enableExtendedQueryTags ? await _queryTagCache.GetAsync(forceRefresh, cancellationToken) : CoreQueryTags;
        }

        private async Task<List<QueryTag>> ResolveQueryTagsAsync(CancellationToken cancellationToken)
        {
            var tags = new List<QueryTag>(CoreQueryTags);
            IReadOnlyList<ExtendedQueryTagStoreEntry> extendedQueryTags = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(cancellationToken: cancellationToken);
            tags.AddRange(extendedQueryTags.Select(entry => new QueryTag(entry)));

            return tags;
        }
    }
}
