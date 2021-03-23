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
        public static readonly IReadOnlyList<QueryTag> CoreIndexTags = GetCoreIndexTags();
        private List<QueryTag> _allIndexTags;
        private int _allIndexTagsStatus;
        private TaskCompletionSource<bool> _allIndexTagsCompletionSource = new TaskCompletionSource<bool>();

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
                if (Interlocked.CompareExchange(ref _allIndexTagsStatus, 1, 0) == 0)
                {
                    _allIndexTags = new List<QueryTag>(CoreIndexTags);

                    IReadOnlyList<ExtendedQueryTagStoreEntry> extendedQueryTagEntries = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(cancellationToken: cancellationToken);
                    _allIndexTags.AddRange(extendedQueryTagEntries.Select(entry => new QueryTag(entry)));

                    _allIndexTagsCompletionSource.SetResult(true);
                }

                await _allIndexTagsCompletionSource.Task;
                return _allIndexTags;
            }
            else
            {
                return CoreIndexTags;
            }
        }

        private static IReadOnlyList<QueryTag> GetCoreIndexTags()
        {
            List<QueryTag> coreTags = new List<QueryTag>();
            coreTags.AddRange(QueryLimit.AllStudiesTags.Select(tag => new QueryTag(tag, QueryTagLevel.Study)));
            coreTags.AddRange(QueryLimit.StudySeriesTags.Select(tag => new QueryTag(tag, QueryTagLevel.Series)));
            coreTags.AddRange(QueryLimit.StudySeriesInstancesTags.Select(tag => new QueryTag(tag, QueryTagLevel.Instance)));
            return coreTags;
        }
    }
}
