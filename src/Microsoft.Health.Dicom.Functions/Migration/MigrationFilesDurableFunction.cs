// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Retrieve;

namespace Microsoft.Health.Dicom.Functions.Migration;

/// <summary>
/// Represents the Azure Durable Functions that perform the migrating frame range files operation
/// based on new tags configured by the user.
/// </summary>
public partial class MigrationFilesDurableFunction
{
    private readonly IInstanceStore _instanceStore;
    private readonly IMetadataStore _metadataStore;
    private readonly MigrationFilesOptions _options;

    public MigrationFilesDurableFunction(
        IInstanceStore instanceStore,
        IMetadataStore metadataStore,
        IOptions<MigrationFilesOptions> configOptions)
    {
        _instanceStore = EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _options = EnsureArg.IsNotNull(configOptions?.Value, nameof(configOptions));
    }
}
