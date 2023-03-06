// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Functions.Update;

/// <summary>
/// Represents the Azure Durable Functions that perform the re-indexing of previously added DICOM instances
/// based on new tags configured by the user.
/// </summary>
public partial class UpdateDurableFunction
{
    private readonly IIndexDataStore _indexStore;
    private readonly UpdateOptions _options;
    private readonly IQueryStore _queryStore;
    private readonly IMetadataStore _metadataStore;
    private readonly IFileStore _fileStore;

    public UpdateDurableFunction(
        IQueryStore queryStore,
        IIndexDataStore indexStore,
        IOptions<UpdateOptions> configOptions,
        IMetadataStore metadataStore,
        IFileStore fileStore)
    {
        _queryStore = EnsureArg.IsNotNull(queryStore, nameof(queryStore));
        _indexStore = EnsureArg.IsNotNull(indexStore, nameof(indexStore));
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _fileStore = EnsureArg.IsNotNull(fileStore, nameof(fileStore));
        _options = EnsureArg.IsNotNull(configOptions?.Value, nameof(configOptions));
    }
}
