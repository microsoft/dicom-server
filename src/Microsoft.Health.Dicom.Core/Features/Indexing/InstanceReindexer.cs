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
        private readonly IStoreFactory<IIndexDataStore> _indexDataStoreFactory;

        public InstanceReindexer(IMetadataStore metadataStore, IStoreFactory<IIndexDataStore> indexDataStoreFactory)
        {
            _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
            _indexDataStoreFactory = EnsureArg.IsNotNull(indexDataStoreFactory, nameof(indexDataStoreFactory));
        }
        public async Task ReindexInstanceAsync(IReadOnlyCollection<ExtendedQueryTagStoreEntry> entries, VersionedInstanceIdentifier versionedInstanceId, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(entries, nameof(entries));
            EnsureArg.IsNotNull(versionedInstanceId, nameof(versionedInstanceId));
            DicomDataset dataset = await _metadataStore.GetInstanceMetadataAsync(versionedInstanceId, cancellationToken);
            var indexDataStore = await _indexDataStoreFactory.GetInstanceAsync(cancellationToken);
            await indexDataStore.ReindexInstanceAsync(dataset, entries.Select(x => new QueryTag(x)), cancellationToken);
        }
    }
}
