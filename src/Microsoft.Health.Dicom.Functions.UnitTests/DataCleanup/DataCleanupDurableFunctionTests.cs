// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Functions.DataCleanup;
using Microsoft.Health.Operations.Functions.DurableTask;
using NSubstitute;

namespace Microsoft.Health.Dicom.Functions.UnitTests.DataCleanup;

public partial class DataCleanupDurableFunctionTests
{
    private readonly DataCleanupDurableFunction _dataCleanupDurableFunction;
    private readonly IInstanceStore _instanceStore;
    private readonly IIndexDataStore _indexStore;
    private readonly IMetadataStore _metadataStore;
    private readonly DataCleanupOptions _options;

    public DataCleanupDurableFunctionTests()
    {
        _instanceStore = Substitute.For<IInstanceStore>();
        _indexStore = Substitute.For<IIndexDataStore>();
        _metadataStore = Substitute.For<IMetadataStore>();
        _options = new DataCleanupOptions { RetryOptions = new ActivityRetryOptions() };
        _dataCleanupDurableFunction = new DataCleanupDurableFunction(
            _instanceStore,
            _indexStore,
            _metadataStore,
            Options.Create(_options));
    }
}
