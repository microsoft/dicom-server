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
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Query;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class IndexTagService : IIndexTagService
    {
        private readonly ICustomTagStore _customTagStore;
        private readonly bool _enableCustomQueryTags;
        public static readonly IReadOnlyList<IndexTag> CoreIndexTags = GetCoreIndexTags();
        private List<IndexTag> _allIndexTags;
        private int _allIndexTagsStatus;
        private TaskCompletionSource<bool> _allIndexTagsCompletionSource = new TaskCompletionSource<bool>();

        public IndexTagService(ICustomTagStore customTagStore, IOptions<FeatureConfiguration> featureConfiguration)
        {
            EnsureArg.IsNotNull(customTagStore, nameof(customTagStore));
            EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));
            _customTagStore = customTagStore;
            _enableCustomQueryTags = featureConfiguration.Value.EnableCustomQueryTags;
        }

        public async Task<IReadOnlyCollection<IndexTag>> GetIndexTagsAsync(CancellationToken cancellationToken = default)
        {
            if (_enableCustomQueryTags)
            {
                if (Interlocked.CompareExchange(ref _allIndexTagsStatus, 1, 0) == 0)
                {
                    _allIndexTags = new List<IndexTag>(CoreIndexTags);

                    IReadOnlyList<CustomTagStoreEntry> customTagEntries = await _customTagStore.GetCustomTagsAsync(cancellationToken: cancellationToken);
                    _allIndexTags.AddRange(customTagEntries.Select(entry => entry.Convert()));

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
            coreTags.AddRange(QueryLimit.AllStudiesTags.Select(tag => tag.Convert(CustomTagLevel.Study)));
            coreTags.AddRange(QueryLimit.StudySeriesTags.Select(tag => tag.Convert(CustomTagLevel.Series)));
            coreTags.AddRange(QueryLimit.StudySeriesInstancesTags.Select(tag => tag.Convert(CustomTagLevel.Instance)));
            return coreTags;
        }
    }
}
