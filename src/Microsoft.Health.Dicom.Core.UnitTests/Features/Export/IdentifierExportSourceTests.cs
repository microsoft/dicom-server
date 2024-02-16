// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Models.Common;
using Microsoft.Health.Dicom.Core.Models.Export;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Export;

public class IdentifierExportSourceTests
{
    private readonly IInstanceStore _store;
    private readonly Partition _partition;
    private readonly IdentifierExportOptions _options;

    public IdentifierExportSourceTests()
    {
        _store = Substitute.For<IInstanceStore>();
        _partition = new Partition(99, "test");
        _options = new IdentifierExportOptions();
    }

    [Fact]
    public async Task GivenInstances_WhenEnumerating_ThenYieldValues()
    {
        // Configure input
        _options.Values = new DicomIdentifier[]
        {
            DicomIdentifier.ForStudy("10"),
            DicomIdentifier.ForStudy("11"),
            DicomIdentifier.ForSeries("100", "200"),
            DicomIdentifier.ForSeries("100", "201"),
            DicomIdentifier.ForInstance("1000", "2000", "3000"),
            DicomIdentifier.ForInstance("1000", "2000", "3001"),
        };

        using var tokenSource = new CancellationTokenSource();
        await using var source = new IdentifierExportSource(_store, _partition, _options);

        var expected = new[]
        {
            new InstanceMetadata(new VersionedInstanceIdentifier("10", "10", "10", 1, _partition), new InstanceProperties()),
            new InstanceMetadata(new VersionedInstanceIdentifier("10", "10", "20", 3, _partition), new InstanceProperties()),
            new InstanceMetadata(new VersionedInstanceIdentifier("10", "20", "10", 1, _partition), new InstanceProperties()),
            new InstanceMetadata(new VersionedInstanceIdentifier("100", "200", "300", 2, _partition), new InstanceProperties()),
            new InstanceMetadata(new VersionedInstanceIdentifier("100", "200", "400", 7, _partition), new InstanceProperties()),
            new InstanceMetadata(new VersionedInstanceIdentifier("100", "200", "500", 2, _partition), new InstanceProperties()),
            new InstanceMetadata(new VersionedInstanceIdentifier("1000", "2000", "3000", 1, _partition), new InstanceProperties()),
        };
        _store
            .GetInstanceIdentifierWithPropertiesAsync(_partition, "10", null, null, false, tokenSource.Token)
            .Returns(expected[..3]);
        _store
            .GetInstanceIdentifierWithPropertiesAsync(_partition, "11", null, null, false, tokenSource.Token)
            .Returns(Array.Empty<InstanceMetadata>());

        _store
            .GetInstanceIdentifierWithPropertiesAsync(_partition, "100", "200", null, false, tokenSource.Token)
            .Returns(expected[3..6]);
        _store
            .GetInstanceIdentifierWithPropertiesAsync(_partition, "100", "201", null, false, tokenSource.Token)
            .Returns(Array.Empty<InstanceMetadata>());

        _store
            .GetInstanceIdentifierWithPropertiesAsync(_partition, "1000", "2000", "3000", false, tokenSource.Token)
            .Returns(new[] { expected[6] });
        _store
            .GetInstanceIdentifierWithPropertiesAsync(_partition, "1000", "2000", "3001", false, tokenSource.Token)
            .Returns(Array.Empty<InstanceMetadata>());

        // Enumerate
        var failures = new List<ReadFailureEventArgs>();
        source.ReadFailure += (source, args) => failures.Add(args);
        ReadResult[] actual = await source.ToArrayAsync(tokenSource.Token);

        // Check Results
        await _store
            .Received(1)
            .GetInstanceIdentifierWithPropertiesAsync(_partition, "10", null, null, false, tokenSource.Token);
        await _store
            .Received(1)
            .GetInstanceIdentifierWithPropertiesAsync(_partition, "11", null, null, false, tokenSource.Token);
        await _store
            .Received(1)
            .GetInstanceIdentifierWithPropertiesAsync(_partition, "100", "200", null, false, tokenSource.Token);
        await _store
            .Received(1)
            .GetInstanceIdentifierWithPropertiesAsync(_partition, "100", "201", null, false, tokenSource.Token);
        await _store
            .Received(1)
            .GetInstanceIdentifierWithPropertiesAsync(_partition, "1000", "2000", "3000", false, tokenSource.Token);
        await _store
            .Received(1)
            .GetInstanceIdentifierWithPropertiesAsync(_partition, "1000", "2000", "3001", false, tokenSource.Token);

        Assert.Same(expected[0], actual[0].Instance);
        Assert.Same(expected[1], actual[1].Instance);
        Assert.Same(expected[2], actual[2].Instance);
        Assert.Equal(DicomIdentifier.ForStudy("11"), actual[3].Failure.Identifier);
        Assert.Same(expected[3], actual[4].Instance);
        Assert.Same(expected[4], actual[5].Instance);
        Assert.Same(expected[5], actual[6].Instance);
        Assert.Equal(DicomIdentifier.ForSeries("100", "201"), actual[7].Failure.Identifier);
        Assert.Same(expected[6], actual[8].Instance);
        Assert.Equal(DicomIdentifier.ForInstance("1000", "2000", "3001"), actual[9].Failure.Identifier);

        // Check event
        Assert.Equal(3, failures.Count);
        Assert.Equal(DicomIdentifier.ForStudy("11"), failures[0].Identifier);
        Assert.Equal(DicomIdentifier.ForSeries("100", "201"), failures[1].Identifier);
        Assert.Equal(DicomIdentifier.ForInstance("1000", "2000", "3001"), failures[2].Identifier);
    }

    [Theory]
    [InlineData(5, 0, null)]
    [InlineData(100, 3, "1", "2", "3")]
    [InlineData(2, 2, "4", "5", "6", "7")]
    public async Task GivenSource_WhenFetchingBatch_ThenRemoveFromSource(int size, int expected, params string[] values)
    {
        DicomIdentifier[] identifierValues = values?.Select(DicomIdentifier.Parse).ToArray() ?? Array.Empty<DicomIdentifier>();
        _options.Values = identifierValues;

        using var tokenSource = new CancellationTokenSource();
        await using var source = new IdentifierExportSource(_store, _partition, _options);

        // Assert baseline
        if (expected == 0)
            Assert.Null(source.Description);
        else
            AssertConfiguration(identifierValues, source.Description);

        // Dequeue a batch
        ExportDataOptions<ExportSourceType> batch;
        if (expected == 0)
        {
            Assert.False(source.TryDequeueBatch(size, out batch));
            Assert.Null(batch);
        }
        else
        {
            Assert.True(source.TryDequeueBatch(size, out batch));
            AssertConfiguration(identifierValues.Take(expected), batch);

            if (identifierValues.Length > expected)
                AssertConfiguration(identifierValues.Skip(expected), source.Description);
            else
                Assert.Null(source.Description);
        }
    }

    private static void AssertConfiguration(IEnumerable<DicomIdentifier> expected, ExportDataOptions<ExportSourceType> actual)
    {
        Assert.Equal(ExportSourceType.Identifiers, actual.Type);

        var options = actual.Settings as IdentifierExportOptions;
        Assert.True(options.Values.SequenceEqual(expected));
    }
}
