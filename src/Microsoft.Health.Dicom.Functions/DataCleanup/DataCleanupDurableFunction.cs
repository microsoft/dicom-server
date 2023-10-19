// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Functions.DataCleanup;

public partial class DataCleanupDurableFunction
{
    private readonly IInstanceStore _instanceStore;
    private readonly IIndexDataStore _indexDataStore;
    private readonly IMetadataStore _metadataStore;
    private readonly DataCleanupOptions _options;

    public DataCleanupDurableFunction(
        IInstanceStore instanceStore,
        IIndexDataStore indexDataStore,
        IMetadataStore metadataStore,
        IOptions<DataCleanupOptions> configOptions)
    {
        _instanceStore = EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
        _indexDataStore = EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _options = EnsureArg.IsNotNull(configOptions?.Value, nameof(configOptions));
    }
}
