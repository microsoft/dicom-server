// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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

        public async Task IndexInstanceAsync(IEnumerable<CustomTagStoreEntry> customTagStoreEntries, VersionedInstanceIdentifier instance, CancellationToken cancellationToken = default)
        {
            // TODO:  GetInstanceMetadataAsync could fail, once move to job framework, resumeing the job should be able to solve this problem. Double check after moving.
            if (customTagStoreEntries.Count() == 0)
            {
                return;
            }

            DicomDataset dataset = await _metadataStore.GetInstanceMetadataAsync(instance);
            Dictionary<long, DicomItem> indexes = dataset.GetCustomTagIndexes(customTagStoreEntries);
            if (indexes.Count == 0)
            {
                return;
            }

            // TODO:  AddCustomTagIndexes could fail, once move to job framework, resumeing the job should be able to solve this problem. Double check after moving.
            await _customTagIndexService.AddCustomTagIndexes(indexes, instance, cancellationToken);
        }
    }
}
