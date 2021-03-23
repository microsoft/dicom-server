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
    public class IndexTagService : IIndexTagService
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly bool _enableExtendedQueryTags;
        public static readonly IReadOnlyList<IndexTag> CoreIndexTags = GetCoreIndexTags();
        private List<IndexTag> _allIndexTags;
        private int _allIndexTagsStatus;
        private TaskCompletionSource<bool> _allIndexTagsCompletionSource = new TaskCompletionSource<bool>();

        public IndexTagService(IExtendedQueryTagStore extendedQueryTagStore, IOptions<FeatureConfiguration> featureConfiguration)
        {
            EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));
            _extendedQueryTagStore = extendedQueryTagStore;
            _enableExtendedQueryTags = featureConfiguration.Value.EnableExtendedQueryTags;
        }

        public async Task<IReadOnlyCollection<IndexTag>> GetIndexTagsAsync(CancellationToken cancellationToken = default)
        {
            if (_enableExtendedQueryTags)
            {
                if (Interlocked.CompareExchange(ref _allIndexTagsStatus, 1, 0) == 0)
                {
                    _allIndexTags = new List<IndexTag>(CoreIndexTags);

                    IReadOnlyList<ExtendedQueryTagStoreEntry> extendedQueryTagEntries = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(cancellationToken: cancellationToken);
                    _allIndexTags.AddRange(extendedQueryTagEntries.Select(entry => new IndexTag(entry)));

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

        private static IReadOnlyList<IndexTag> GetCoreIndexTags()
        {
            List<IndexTag> coreTags = new List<IndexTag>();
            coreTags.AddRange(QueryLimit.AllStudiesTags.Select(tag => new IndexTag(tag, ExtendedQueryTagLevel.Study)));
            coreTags.AddRange(QueryLimit.StudySeriesTags.Select(tag => new IndexTag(tag, ExtendedQueryTagLevel.Series)));
            coreTags.AddRange(QueryLimit.StudySeriesInstancesTags.Select(tag => new IndexTag(tag, ExtendedQueryTagLevel.Instance)));
            return coreTags;
        }
    }
}
