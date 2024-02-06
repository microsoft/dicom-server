// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Functions.MetricsCollection;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.IndexMetricsCollection;

public class IndexMetricsCollectionFunctionTests
{
    private readonly IndexMetricsCollectionFunction _collectionFunction;
    private readonly IIndexDataStore _indexStore;
    private readonly TimerInfo _timer;

    public IndexMetricsCollectionFunctionTests()
    {
        _indexStore = Substitute.For<IIndexDataStore>();
        _collectionFunction = new IndexMetricsCollectionFunction(
            _indexStore,
            Options.Create(new FeatureConfiguration { EnableExternalStore = true, }));
        _timer = Substitute.For<TimerInfo>(default, default, default);
    }

    [Fact]
    public async Task GivenIndexMetricsCollectionFunction_WhenRun_CollectionExecutedWhenExternalStoreEnabled()
    {
        _indexStore.GetIndexedFileMetricsAsync().ReturnsForAnyArgs(new IndexedFileProperties());

        await _collectionFunction.Run(_timer, NullLogger.Instance);

        await _indexStore.ReceivedWithAnyArgs(1).GetIndexedFileMetricsAsync();
    }

    [Fact]
    public async Task GivenIndexMetricsCollectionFunction_WhenRun_CollectionNotExecutedWhenExternalStoreNotEnabled()
    {
        _indexStore.GetIndexedFileMetricsAsync().ReturnsForAnyArgs(new IndexedFileProperties());
        var collectionFunctionWihtoutExternalStore = new IndexMetricsCollectionFunction(
            _indexStore,
            Options.Create(new FeatureConfiguration { EnableExternalStore = false, }));

        await collectionFunctionWihtoutExternalStore.Run(_timer, NullLogger.Instance);

        await _indexStore.DidNotReceiveWithAnyArgs().GetIndexedFileMetricsAsync();
    }
}
