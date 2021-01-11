// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class InstanceIndexer : IInstanceIndexer
    {
        private readonly IMetadataStore _metadataStore;
        private readonly ICustomTagIndexService _customTagIndexService;

        public InstanceIndexer(IMetadataStore metadataStore, ICustomTagIndexService customTagIndexService)
        {
            _metadataStore = metadataStore;
            _customTagIndexService = customTagIndexService;
        }

        public async Task IndexInstanceAsync(IEnumerable<CustomTagEntry> customTags, VersionedInstanceIdentifier instance, CancellationToken cancellationToken = default)
        {
            //TODO: it's possible that we couldn't get the metadata -- it's deleted or not able to connect to blob (how should we handle with this)
            DicomDataset dataset = await _metadataStore.GetInstanceMetadataAsync(instance);

            // We only support index on top level custom tag for now, will support embeded tag later.
            Dictionary<long, DicomItem> indexes = new Dictionary<long, DicomItem>();
            Dictionary<string, CustomTagEntry> tagPathDictionary = customTags.ToDictionary(keySelector: item => item.Path, comparer: StringComparer.OrdinalIgnoreCase);

            foreach (var item in dataset)
            {
                string path = item.Tag.GetPath();
                if (tagPathDictionary.ContainsKey(path))
                {
                    CustomTagEntry entry = tagPathDictionary[path];

                    // check if VR match
                    if (string.Equals(entry.VR, item.ValueRepresentation.Code, StringComparison.OrdinalIgnoreCase))
                    {
                        indexes[entry.Key] = item;
                    }
                }
            }

            await _customTagIndexService.AddCustomTagIndexes(indexes, instance, cancellationToken);
        }
    }
}
