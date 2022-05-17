// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.Common;
using Microsoft.Health.Dicom.Core.Models.Export;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Export;

public class IdentifierExportSourceTests
{
    private readonly IInstanceStore _store;
    private readonly PartitionEntry _partition;
    private readonly IdentifierExportOptions _options;
    private readonly IdentifierExportSource _source;

    public IdentifierExportSourceTests()
    {
        _store = Substitute.For<IInstanceStore>();
        _partition = new PartitionEntry(99, "test");
        _options = new IdentifierExportOptions();
        _source = new IdentifierExportSource(_store, _partition, Options.Create(_options));
    }

    [Fact]
    public async Task GivenInstances_WhenEnumerating_ThenYieldValues()
    {
        using var tokenSource = new CancellationTokenSource();

        // Configure input
        var identifiers = new DicomIdentifier[]
        {
            DicomIdentifier.ForStudy("10"),
            DicomIdentifier.ForStudy("11"),
            DicomIdentifier.ForSeries("100", "200"),
            DicomIdentifier.ForSeries("100", "201"),
            DicomIdentifier.ForInstance("1000", "2000", "3000"),
            DicomIdentifier.ForInstance("1000", "2000", "3001"),
        };

        var expected = new VersionedInstanceIdentifier[]
        {
            new VersionedInstanceIdentifier("10", "10", "10", 1, _partition.PartitionKey),
            new VersionedInstanceIdentifier("10", "10", "20", 3, _partition.PartitionKey),
            new VersionedInstanceIdentifier("10", "20", "10", 1, _partition.PartitionKey),
            new VersionedInstanceIdentifier("100", "200", "300", 2, _partition.PartitionKey),
            new VersionedInstanceIdentifier("100", "200", "400", 7, _partition.PartitionKey),
            new VersionedInstanceIdentifier("100", "200", "500", 2, _partition.PartitionKey),
            new VersionedInstanceIdentifier("1000", "2000", "3000", 1, _partition.PartitionKey),
        };

        _options.Values = identifiers.Select(x => x.ToString()).ToArray();
        _store
            .GetInstanceIdentifiersInStudyAsync(_partition.PartitionKey, "10", tokenSource.Token)
            .Returns(expected[0..3]);
        _store
            .GetInstanceIdentifiersInStudyAsync(_partition.PartitionKey, "11", tokenSource.Token)
            .Returns(Array.Empty<VersionedInstanceIdentifier>());
        _store
            .GetInstanceIdentifiersInSeriesAsync(_partition.PartitionKey, "100", "200", tokenSource.Token)
            .Returns(expected[3..6]);
        _store
            .GetInstanceIdentifiersInSeriesAsync(_partition.PartitionKey, "100", "201", tokenSource.Token)
            .Returns(Array.Empty<VersionedInstanceIdentifier>());
        _store
            .GetInstanceIdentifierAsync(_partition.PartitionKey, "1000", "2000", "3000", tokenSource.Token)
            .Returns(new VersionedInstanceIdentifier[] { expected[6] });
        _store
            .GetInstanceIdentifierAsync(_partition.PartitionKey, "1000", "2000", "3001", tokenSource.Token)
            .Returns(Array.Empty<VersionedInstanceIdentifier>());

        // Enumerate
        var failures = new List<ReadFailureEventArgs>();
        _source.ReadFailure += (source, args) => failures.Add(args);
        ReadResult[] actual = await _source.ToArrayAsync(tokenSource.Token);

        // Check Results
        await _store
            .Received(1)
            .GetInstanceIdentifiersInStudyAsync(_partition.PartitionKey, "10", tokenSource.Token);
        await _store
            .Received(1)
            .GetInstanceIdentifiersInStudyAsync(_partition.PartitionKey, "11", tokenSource.Token);
        await _store
            .Received(1)
            .GetInstanceIdentifiersInSeriesAsync(_partition.PartitionKey, "100", "200", tokenSource.Token);
        await _store
            .Received(1)
            .GetInstanceIdentifiersInSeriesAsync(_partition.PartitionKey, "100", "201", tokenSource.Token);
        await _store
            .Received(1)
            .GetInstanceIdentifierAsync(_partition.PartitionKey, "1000", "2000", "3000", tokenSource.Token);
        await _store
            .Received(1)
            .GetInstanceIdentifierAsync(_partition.PartitionKey, "1000", "2000", "3001", tokenSource.Token);

        Assert.Same(expected[0], actual[0].Identifier);
        Assert.Same(expected[1], actual[1].Identifier);
        Assert.Same(expected[2], actual[2].Identifier);
        Assert.Equal(DicomIdentifier.ForStudy("11"), actual[3].Failure.Identifier);
        Assert.Same(expected[3], actual[4].Identifier);
        Assert.Same(expected[4], actual[5].Identifier);
        Assert.Same(expected[5], actual[6].Identifier);
        Assert.Equal(DicomIdentifier.ForSeries("100", "201"), actual[7].Failure.Identifier);
        Assert.Same(expected[6], actual[8].Identifier);
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
    public void GivenSource_WhenFetchingBatch_ThenRemoveFromSource(int size, int expected, params string[] values)
    {
        _options.Values = values ?? Array.Empty<string>();

        // Assert baseline
        if (_options.Values.Count > 0)
            AssertConfiguration(values, _source.Description);
        else
            Assert.Null(_source.Description);

        // Dequeue a batch
        TypedConfiguration<ExportSourceType> batch;
        if (expected == 0)
        {
            Assert.False(_source.TryDequeueBatch(size, out batch));
            Assert.Null(batch);
            Assert.Null(_source.Description);
        }
        else
        {
            Assert.True(_source.TryDequeueBatch(size, out batch));
            AssertConfiguration(values.Take(expected), batch);

            if (values.Length > expected)
                AssertConfiguration(values.Skip(expected), _source.Description);
            else
                Assert.Null(_source.Description);
        }
    }

    private static void AssertConfiguration(IEnumerable<string> expected, TypedConfiguration<ExportSourceType> actual)
    {
        Assert.Equal(ExportSourceType.Identifiers, actual.Type);

        IdentifierExportOptions options = actual.Configuration.Get<IdentifierExportOptions>();
        Assert.True(options.Values.SequenceEqual(expected));
    }
}
