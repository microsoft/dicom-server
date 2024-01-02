// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.ContentLengthBackFill.Models;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.ContentLengthBackFill;

public partial class ContentLengthBackFillDurableFunctionTests
{
    [Fact]
    public async Task GivenBatch_WhenGettingInstanceBatches_ThenShouldInvokeCorrectMethodWithCorrectParams()
    {
        const int batchSize = 100;
        const int maxParallelBatches = 3;

        IReadOnlyList<WatermarkRange> expected = new List<WatermarkRange> { new WatermarkRange(12345, 678910) };
        _instanceStore
            .GetContentLengthBackFillInstanceBatches(batchSize, maxParallelBatches, CancellationToken.None)
            .Returns(expected);

        IReadOnlyList<WatermarkRange> actual = await _contentLengthBackFillDurableFunction.GetInstanceBatches(
            new BatchCreationArguments(batchSize, maxParallelBatches),
            NullLogger.Instance);

        Assert.Same(expected, actual);
        await _instanceStore
            .Received(1)
            .GetContentLengthBackFillInstanceBatches(batchSize, maxParallelBatches, CancellationToken.None);
    }

    [Fact]
    public async Task GivenBatch_WhenBackFillContentLengthRangeDataAsync_ThenShouldBackfillEachInstance()
    {
        var watermarkRange = new WatermarkRange(3, 10);

        var expected = new List<VersionedInstanceIdentifier>
        {
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 3),
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 4),
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 5),
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 6),
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 7),
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 8),
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 9),
        };

        // Arrange input
        _instanceStore
            .GetContentLengthBackFillInstanceIdentifiersByWatermarkRangeAsync(watermarkRange, Arg.Any<CancellationToken>())
            .Returns(expected);

        var expectedFileProperty = new FileProperties { ContentLength = 123, Path = "new path", ETag = "new etag" };
        foreach (VersionedInstanceIdentifier identifier in expected)
        {
            _fileStore.GetFilePropertiesAsync(
                    identifier.Version,
                    identifier.Partition,
                    fileProperties: null,
                    Arg.Any<CancellationToken>())
                .Returns(expectedFileProperty);
        }

        Dictionary<long, FileProperties> expectedFilePropertiesByWatermark = new Dictionary<long, FileProperties>();
        foreach (var x in expected)
        {
            expectedFilePropertiesByWatermark.TryAdd(x.Version, expectedFileProperty);
        }

        _indexStore.UpdateFilePropertiesContentLengthAsync(expectedFilePropertiesByWatermark).Returns(Task.CompletedTask);

        // Call the activity
        await _contentLengthBackFillDurableFunction.BackFillContentLengthRangeDataAsync(watermarkRange, NullLogger.Instance);

        // Assert behavior
        await _instanceStore
            .Received(1)
            .GetContentLengthBackFillInstanceIdentifiersByWatermarkRangeAsync(watermarkRange, CancellationToken.None);

        foreach (VersionedInstanceIdentifier identifier in expected)
        {
            await _fileStore
                .Received(1)
                .GetFilePropertiesAsync(identifier.Version, identifier.Partition, fileProperties: null,
                    Arg.Any<CancellationToken>());
        }

        await _indexStore.Received(1).UpdateFilePropertiesContentLengthAsync(Arg.Is<IReadOnlyDictionary<long, FileProperties>>(x =>
            x.Keys.SequenceEqual(expected.Select(y => y.Version)) &&
            x.Values.All(y => y.ContentLength == expectedFileProperty.ContentLength)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GivenBatch_WhenInstanceBlobLengthIsLessThanOne_ThenExpectInstanceNotUpdated(long corruptedContentLength)
    {
        var watermarkRange = new WatermarkRange(3, 10);

        var expected = new List<VersionedInstanceIdentifier>
        {
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 3),
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 4),
            new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 5)
        };

        // Arrange input
        _instanceStore
            .GetContentLengthBackFillInstanceIdentifiersByWatermarkRangeAsync(watermarkRange, Arg.Any<CancellationToken>())
            .Returns(expected);

        var expectedFileProperty = new FileProperties { ContentLength = 123, Path = "new path", ETag = "new etag" };

        // we will sandwich the corrupted data in between good data
        _fileStore.GetFilePropertiesAsync(expected[0].Version, expected[0].Partition, fileProperties: null, Arg.Any<CancellationToken>())
            .Returns(expectedFileProperty);

        _fileStore.GetFilePropertiesAsync(expected[1].Version, expected[1].Partition, fileProperties: null, Arg.Any<CancellationToken>())
            .Returns(new FileProperties { ContentLength = corruptedContentLength, Path = "new path", ETag = "new etag" });

        _fileStore.GetFilePropertiesAsync(expected[2].Version, expected[2].Partition, fileProperties: null, Arg.Any<CancellationToken>())
            .Returns(expectedFileProperty);


        // Only non-corrupted data will be updated
        IReadOnlyDictionary<long, FileProperties> expectedFilePropertiesByWatermark = new Dictionary<long, FileProperties>()
        {
            [expected[0].Version] = expectedFileProperty,
            [expected[2].Version] = expectedFileProperty
        };

        _indexStore.UpdateFilePropertiesContentLengthAsync(expectedFilePropertiesByWatermark).Returns(Task.CompletedTask);

        // Call the activity
        await _contentLengthBackFillDurableFunction.BackFillContentLengthRangeDataAsync(watermarkRange, NullLogger.Instance);

        // Assert behavior
        await _instanceStore
            .Received(1)
            .GetContentLengthBackFillInstanceIdentifiersByWatermarkRangeAsync(watermarkRange, CancellationToken.None);

        foreach (VersionedInstanceIdentifier identifier in expected)
        {
            await _fileStore
                .Received(1)
                .GetFilePropertiesAsync(identifier.Version, identifier.Partition, fileProperties: null, Arg.Any<CancellationToken>());
        }

        await _indexStore.Received(1).UpdateFilePropertiesContentLengthAsync(Arg.Is<IReadOnlyDictionary<long, FileProperties>>(x =>
            x.Keys.SequenceEqual(expectedFilePropertiesByWatermark.Keys) &&
            x.Values.All(y => y.ContentLength == expectedFileProperty.ContentLength)));
    }
}
