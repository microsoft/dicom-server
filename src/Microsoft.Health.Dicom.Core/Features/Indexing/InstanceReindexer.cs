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
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    /// <summary>
    /// Represents an Reindexer which reindexes DICOM instance.
    /// </summary>
    public class InstanceReindexer : IInstanceReindexer
    {
        private readonly IMetadataStore _metadataStore;
        private readonly IIndexDataStore _indexDataStore;
        private readonly IDicomDatasetReindexValidator _dicomDatasetReindexValidator;

        public InstanceReindexer(IMetadataStore metadataStore, IIndexDataStore indexDataStore, IDicomDatasetReindexValidator dicomDatasetReindexValidator)
        {
            _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
            _indexDataStore = EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
            _dicomDatasetReindexValidator = EnsureArg.IsNotNull(dicomDatasetReindexValidator, nameof(dicomDatasetReindexValidator));
        }

        public async Task ReindexInstanceAsync(IReadOnlyCollection<ExtendedQueryTagStoreEntry> entries, VersionedInstanceIdentifier versionedInstanceId, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(entries, nameof(entries));
            EnsureArg.IsNotNull(versionedInstanceId, nameof(versionedInstanceId));
            if (entries.Count == 0)
            {
                return;
            }

            DicomDataset dataset = await _metadataStore.GetInstanceMetadataAsync(versionedInstanceId, cancellationToken);

            // Only reindex on valid query tags
            var validQueryTags = _dicomDatasetReindexValidator.Validate(dataset, entries.Select(x => new QueryTag(x)).ToList());
            await _indexDataStore.ReindexInstanceAsync(dataset, validQueryTags, cancellationToken);
        }
    }
}
