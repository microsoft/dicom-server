// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Core.Features.Indexing;

/// <summary>
/// Represents an Reindexer which reindexes DICOM instance.
/// </summary>
public class InstanceReindexer : IInstanceReindexer
{
    private readonly IMetadataStore _metadataStore;
    private readonly IIndexDataStore _indexDataStore;
    private readonly IReindexDatasetValidator _dicomDatasetReindexValidator;
    private readonly ILogger _logger;

    public InstanceReindexer(
        IMetadataStore metadataStore,
        IIndexDataStore indexDataStore,
        IReindexDatasetValidator dicomDatasetReindexValidator,
        ILogger<InstanceReindexer> logger)
    {
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _indexDataStore = EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
        _dicomDatasetReindexValidator = EnsureArg.IsNotNull(dicomDatasetReindexValidator, nameof(dicomDatasetReindexValidator));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task ReindexInstanceAsync(
        IReadOnlyCollection<ExtendedQueryTagStoreEntry> entries,
        VersionedInstanceIdentifier versionedInstanceId,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(entries, nameof(entries));
        EnsureArg.IsNotNull(versionedInstanceId, nameof(versionedInstanceId));

        DicomDataset dataset = await _metadataStore.GetInstanceMetadataAsync(versionedInstanceId.Version, cancellationToken);

        // Only reindex on valid query tags
        IReadOnlyCollection<QueryTag> validQueryTags = await _dicomDatasetReindexValidator.ValidateAsync(
            dataset,
            versionedInstanceId.Version,
            entries.Select(x => new QueryTag(x)).ToList(),
            cancellationToken);
        await _indexDataStore.ReindexInstanceAsync(dataset, versionedInstanceId.Version, validQueryTags, cancellationToken);
    }
}
