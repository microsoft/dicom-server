// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Features.Update;

namespace Microsoft.Health.Dicom.Functions.Update;

/// <summary>
/// Represents the Azure Durable Functions that perform updating list of instances in multiple studies.
/// </summary>
public partial class UpdateDurableFunction
{
    private readonly IIndexDataStore _indexStore;
    private readonly IInstanceStore _instanceStore;
    private readonly UpdateOptions _options;
    private readonly IMetadataStore _metadataStore;
    private readonly IFileStore _fileStore;
    private readonly IUpdateInstanceService _updateInstanceService;
    private readonly UpdateMeter _updateMeter;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly bool _externalStoreEnabled;

    public UpdateDurableFunction(
        IIndexDataStore indexStore,
        IInstanceStore instanceStore,
        IOptions<UpdateOptions> configOptions,
        IMetadataStore metadataStore,
        IFileStore fileStore,
        IUpdateInstanceService updateInstanceService,
        UpdateMeter updateMeter,
        IOptions<JsonSerializerOptions> jsonSerializerOptions,
        IOptions<FeatureConfiguration> featureConfiguration)
    {
        EnsureArg.IsNotNull(featureConfiguration, nameof(featureConfiguration));
        _indexStore = EnsureArg.IsNotNull(indexStore, nameof(indexStore));
        _instanceStore = EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _fileStore = EnsureArg.IsNotNull(fileStore, nameof(fileStore));
        _updateInstanceService = EnsureArg.IsNotNull(updateInstanceService, nameof(updateInstanceService));
        _jsonSerializerOptions = EnsureArg.IsNotNull(jsonSerializerOptions?.Value, nameof(jsonSerializerOptions));
        _updateMeter = EnsureArg.IsNotNull(updateMeter, nameof(updateMeter));
        _options = EnsureArg.IsNotNull(configOptions?.Value, nameof(configOptions));
        _externalStoreEnabled = featureConfiguration.Value.EnableExternalStore;
    }
}
