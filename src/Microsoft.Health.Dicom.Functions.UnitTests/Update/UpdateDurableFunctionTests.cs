// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text.Json;
using FellowOakDicom.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Features.Update;
using Microsoft.Health.Dicom.Core.Serialization;
using Microsoft.Health.Dicom.Functions.Update;
using Microsoft.Health.Operations.Functions.DurableTask;
using NSubstitute;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Update;

public partial class UpdateDurableFunctionTests
{
    private readonly UpdateDurableFunction _updateDurableFunction;
    private readonly UpdateDurableFunction _updateDurableFunctionWithExternalStore;
    private readonly IIndexDataStore _indexStore;
    private readonly IInstanceStore _instanceStore;
    private readonly UpdateOptions _options;
    private readonly IMetadataStore _metadataStore;
    private readonly IFileStore _fileStore;
    private readonly IUpdateInstanceService _updateInstanceService;
    private readonly UpdateMeter _updateMeter;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private MeterProvider _meterProvider;
    private List<Metric> _exportedItems;

    public UpdateDurableFunctionTests()
    {
        _indexStore = Substitute.For<IIndexDataStore>();
        _instanceStore = Substitute.For<IInstanceStore>();
        _metadataStore = Substitute.For<IMetadataStore>();
        _fileStore = Substitute.For<IFileStore>();
        _updateInstanceService = Substitute.For<IUpdateInstanceService>();
        _options = new UpdateOptions { RetryOptions = new ActivityRetryOptions() };
        _jsonSerializerOptions = new JsonSerializerOptions();
        _jsonSerializerOptions.Converters.Add(new DicomJsonConverter(writeTagsAsKeywords: true, autoValidate: false, numberSerializationMode: NumberSerializationMode.PreferablyAsNumber));
        _updateMeter = new UpdateMeter();
        _jsonSerializerOptions.Converters.Add(new ExportDataOptionsJsonConverter());
        _updateDurableFunction = new UpdateDurableFunction(
            _indexStore,
            _instanceStore,
            Options.Create(_options),
            _metadataStore,
            _fileStore,
            _updateInstanceService,
            Substitute.For<IQueryTagService>(),
            _updateMeter,
            Options.Create(_jsonSerializerOptions),
            Options.Create(new FeatureConfiguration()));
        _updateDurableFunctionWithExternalStore = new UpdateDurableFunction(
            _indexStore,
            _instanceStore,
            Options.Create(_options),
            _metadataStore,
            _fileStore,
            _updateInstanceService,
            Substitute.For<IQueryTagService>(),
            _updateMeter,
            Options.Create(_jsonSerializerOptions),
            Options.Create(new FeatureConfiguration { EnableExternalStore = true, }));
        InitializeMetricExporter();
    }

    private void InitializeMetricExporter()
    {
        _exportedItems = new List<Metric>();
        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter($"{OpenTelemetryLabels.BaseMeterName}.Update")
            .AddInMemoryExporter(_exportedItems)
            .Build();
    }
}
