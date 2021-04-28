// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Query;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class QueryTagService : IQueryTagService
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly bool _enableExtendedQueryTags;
        public static readonly IReadOnlyList<QueryTag> CoreQueryTags = GetCoreQueryTags();
        private List<QueryTag> _allQueryTags;
        private int _allQueryTagsStatus;
        private TaskCompletionSource<bool> _allQueryTagsCompletionSource = new TaskCompletionSource<bool>();

        public QueryTagService(IExtendedQueryTagStore extendedQueryTagStore, IOptions<FeatureConfiguration> featureConfiguration)
        {
            EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));
            _extendedQueryTagStore = extendedQueryTagStore;
            _enableExtendedQueryTags = featureConfiguration.Value.EnableExtendedQueryTags;
        }

        public async Task<IReadOnlyCollection<QueryTag>> GetQueryTagsAsync(CancellationToken cancellationToken = default)
        {
            if (_enableExtendedQueryTags)
            {
                if (Interlocked.CompareExchange(ref _allQueryTagsStatus, 1, 0) == 0)
                {
                    _allQueryTags = new List<QueryTag>(CoreQueryTags);

                    IReadOnlyList<ExtendedQueryTagStoreEntry> extendedQueryTagEntries = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(cancellationToken: cancellationToken);
                    _allQueryTags.AddRange(extendedQueryTagEntries.Select(entry => new QueryTag(entry)));

                    _allQueryTagsCompletionSource.SetResult(true);
                }

                await _allQueryTagsCompletionSource.Task;
                return _allQueryTags;
            }
            else
            {
                return CoreQueryTags;
            }
        }

        private static IReadOnlyList<QueryTag> GetCoreQueryTags()
        {
            return QueryLimit.CoreTags.Select(tag => new QueryTag(tag)).ToList();
        }
    }
}
