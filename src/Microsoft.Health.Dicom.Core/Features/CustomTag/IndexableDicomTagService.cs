// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Query;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class IndexableDicomTagService : IIndexableDicomTagService
    {
        private readonly ICustomTagStore _customTagStore;
        private readonly bool _enableCustomQueryTags;
        public static readonly IReadOnlyList<IndexableDicomTag> CoreIndexableDicomTags = GetCoreIndexableDicomTags();
        private List<IndexableDicomTag> _allIndexableDicomTags;

        public IndexableDicomTagService(ICustomTagStore customTagStore, IOptions<FeatureConfiguration> featureConfiguration)
        {
            EnsureArg.IsNotNull(customTagStore, nameof(customTagStore));
            EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));
            _customTagStore = customTagStore;
            _enableCustomQueryTags = featureConfiguration.Value.EnableCustomQueryTags;
        }

        public async Task<IReadOnlyCollection<IndexableDicomTag>> GetIndexableDicomTagsAsync(CancellationToken cancellationToken = default)
        {
            if (_enableCustomQueryTags)
            {
                if (_allIndexableDicomTags == null)
                {
                    _allIndexableDicomTags = new List<IndexableDicomTag>(CoreIndexableDicomTags);

                    IReadOnlyCollection<CustomTagEntry> customTagEntries = await _customTagStore.GetCustomTagsAsync(cancellationToken: cancellationToken);
                    foreach (CustomTagEntry customTagEntry in customTagEntries)
                    {
                        DicomTag tag = DicomTag.Parse(customTagEntry.Path);
                        DicomVR vr = DicomVR.Parse(customTagEntry.VR);
                        _allIndexableDicomTags.Add(new IndexableDicomTag(tag, vr, customTagEntry.Level, isCustomTag: true));
                    }
                }

                return _allIndexableDicomTags;
            }
            else
            {
                return CoreIndexableDicomTags;
            }
        }

        private static IReadOnlyList<IndexableDicomTag> GetCoreIndexableDicomTags()
        {
            List<IndexableDicomTag> coreTags = new List<IndexableDicomTag>();
            coreTags.AddRange(QueryLimit.AllStudiesTags.Select(tag => new IndexableDicomTag(tag, tag.GetDefaultVR(), CustomTagLevel.Study, isCustomTag: false)));
            coreTags.AddRange(QueryLimit.StudySeriesTags.Select(tag => new IndexableDicomTag(tag, tag.GetDefaultVR(), CustomTagLevel.Series, isCustomTag: false)));
            coreTags.AddRange(QueryLimit.StudySeriesInstancesTags.Select(tag => new IndexableDicomTag(tag, tag.GetDefaultVR(), CustomTagLevel.Instance, isCustomTag: false)));
            return coreTags;
        }
    }
}
