// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Functions.Update.Models;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Update;

public partial class UpdateDurableFunctionTests
{
    [Fact]
    public async Task GivenInstanceMetadata_WhenGettingInstances_ThenShouldMatchCorrectly()
    {
        var studyInstanceUid = TestUidGenerator.Generate();
        var identifiers = GetInstanceIdentifiersList(studyInstanceUid);
        _instanceStore.GetInstanceIdentifierWithPropertiesAsync(
            DefaultPartition.Key,
            studyInstanceUid,
            cancellationToken: CancellationToken.None)
            .Returns(identifiers);

        IReadOnlyList<InstanceFileIdentifier> expected = identifiers.Select(x =>
            new InstanceFileIdentifier
            {
                Version = x.VersionedInstanceIdentifier.Version,
                OriginalVersion = x.InstanceProperties.OriginalVersion,
                NewVersion = x.InstanceProperties.NewVersion
            }).ToList();

        IReadOnlyList<InstanceFileIdentifier> actual = await _updateDurableFunction.GetInstanceWatermarksInStudyAsync(
            new GetInstanceArguments(DefaultPartition.Key, studyInstanceUid),
            NullLogger.Instance);

        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Version, actual[i].Version);
            Assert.Equal(expected[i].OriginalVersion, actual[i].OriginalVersion);
            Assert.Equal(expected[i].NewVersion, actual[i].NewVersion);
        }

        await _instanceStore
            .Received(1)
            .GetInstanceIdentifierWithPropertiesAsync(DefaultPartition.Key, studyInstanceUid, cancellationToken: CancellationToken.None);
    }

    [Fact]
    public async Task GivenInstanceMetadata_WhenUpdatingInstanceWatermark_ThenShouldMatchCorrectly()
    {
        var studyInstanceUid = TestUidGenerator.Generate();
        var identifiers = GetInstanceIdentifiersList(studyInstanceUid);
        IReadOnlyList<InstanceFileIdentifier> expected = identifiers.Select(x =>
            new InstanceFileIdentifier
            {
                Version = x.VersionedInstanceIdentifier.Version,
                OriginalVersion = x.InstanceProperties.OriginalVersion,
                NewVersion = x.InstanceProperties.NewVersion
            }).ToList();

        var versions = expected.Select(x => x.Version).ToList();

        var dataset = "{\"00100010\":{\"vr\":\"PN\",\"Value\":[{\"Alphabetic\":\"Patient Name\"}]}}";

        _indexStore.BeginUpdateInstanceAsync(DefaultPartition.Key, Arg.Is<IReadOnlyCollection<long>>(x => x.Count == versions.Count), CancellationToken.None).Returns(identifiers);

        IReadOnlyList<InstanceFileIdentifier> actual = await _updateDurableFunction.UpdateInstanceWatermarkAsync(
            new BatchUpdateArguments(DefaultPartition.Key, expected, dataset),
            NullLogger.Instance);

        await _indexStore
           .Received(1)
           .BeginUpdateInstanceAsync(DefaultPartition.Key, Arg.Is<IReadOnlyCollection<long>>(x => x.Count == versions.Count), cancellationToken: CancellationToken.None);

        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Version, actual[i].Version);
            Assert.Equal(expected[i].OriginalVersion, actual[i].OriginalVersion);
            Assert.Equal(expected[i].NewVersion, actual[i].NewVersion);
        }
    }

    [Fact]
    public async Task GivenInstanceMetadata_WhenUpdatingBlobInBatches_ThenShouldUpdateCorrectly()
    {
        var studyInstanceUid = TestUidGenerator.Generate();
        var identifiers = GetInstanceIdentifiersList(studyInstanceUid);
        IReadOnlyList<InstanceFileIdentifier> expected = identifiers.Select(x =>
            new InstanceFileIdentifier
            {
                Version = x.VersionedInstanceIdentifier.Version,
                OriginalVersion = x.InstanceProperties.OriginalVersion,
                NewVersion = x.InstanceProperties.NewVersion
            }).ToList();

        var versions = expected.Select(x => x.Version).ToList();
        var dataset = "{\"00100010\":{\"vr\":\"PN\",\"Value\":[{\"Alphabetic\":\"Patient Name\"}]}}";

        foreach (var instance in expected)
        {
            _updateInstanceService
                .UpdateInstanceBlobAsync(
                instance,
                Arg.Is<DicomDataset>(x => x.GetSingleValue<string>(DicomTag.PatientName) == "Patient Name"),
                Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
        }

        await _updateDurableFunction.UpdateInstanceBatchAsync(
            new BatchUpdateArguments(DefaultPartition.Key, expected, dataset),
            NullLogger.Instance);

        foreach (var instance in expected)
        {
            await _updateInstanceService
                .Received(1)
                .UpdateInstanceBlobAsync(
                instance,
                Arg.Is<DicomDataset>(x => x.GetSingleValue<string>(DicomTag.PatientName) == "Patient Name"),
                Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task GivenCompleteInstanceArgument_WhenCompleting_ThenShouldComplete()
    {
        var studyInstanceUid = TestUidGenerator.Generate();

        _indexStore.EndUpdateInstanceAsync(DefaultPartition.Key, studyInstanceUid, new DicomDataset(), CancellationToken.None).Returns(Task.CompletedTask);

        var ds = new DicomDataset
        {
            { DicomTag.PatientName, "Patient Name" }
        };

        await _updateDurableFunction.CompleteUpdateInstanceAsync(
            new CompleteInstanceArguments(DefaultPartition.Key, studyInstanceUid, "{\"00100010\":{\"vr\":\"PN\",\"Value\":[{\"Alphabetic\":\"Patient Name\"}]}}"),
            NullLogger.Instance);

        await _indexStore
            .Received(1)
            .EndUpdateInstanceAsync(DefaultPartition.Key, studyInstanceUid, Arg.Is<DicomDataset>(x => x.GetSingleValue<string>(DicomTag.PatientName) == "Patient Name"), CancellationToken.None);
    }

    [Fact]
    public async Task GivenInstanceMetadataList_WhenDeleteFile_ThenShouldDeleteSuccessfully()
    {
        var studyInstanceUid = TestUidGenerator.Generate();
        var identifiers = GetInstanceIdentifiersList(studyInstanceUid, instanceProperty: new InstanceProperties { OriginalVersion = 1 });
        IReadOnlyList<InstanceFileIdentifier> expected = identifiers.Select(x =>
            new InstanceFileIdentifier
            {
                Version = x.VersionedInstanceIdentifier.Version,
                OriginalVersion = x.InstanceProperties.OriginalVersion,
                NewVersion = x.InstanceProperties.NewVersion
            }).Take(1).ToList();

        // Arrange input
        IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
        context.GetInput<IReadOnlyList<InstanceFileIdentifier>>().Returns(expected);

        _updateInstanceService
            .DeleteInstanceBlobAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Call the activity
        await _updateDurableFunction.DeleteOldVersionBlobAsync(
            context,
            NullLogger.Instance);

        // Assert behavior
        context.Received(1).GetInput<IReadOnlyList<InstanceFileIdentifier>>();
        await _updateInstanceService
            .Received(1)
            .DeleteInstanceBlobAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    private static List<InstanceMetadata> GetInstanceIdentifiersList(string studyInstanceUid, int partitionKey = DefaultPartition.Key, InstanceProperties instanceProperty = null)
    {
        var dicomInstanceIdentifiersList = new List<InstanceMetadata>();

        instanceProperty = instanceProperty ?? new InstanceProperties();

        dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate(), 0, partitionKey), instanceProperty));
        dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1, partitionKey), instanceProperty));
        return dicomInstanceIdentifiersList;
    }
}
