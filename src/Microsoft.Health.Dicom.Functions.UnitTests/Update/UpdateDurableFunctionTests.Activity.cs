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
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Functions.Update.Models;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Update;

public partial class UpdateDurableFunctionTests
{
    [Fact]
    public async Task GivenInstanceMetadata_WhenUpdatingInstanceWatermark_ThenShouldMatchCorrectly()
    {
        var studyInstanceUid = TestUidGenerator.Generate();
        var identifiers = GetInstanceIdentifiersList(studyInstanceUid);
        IReadOnlyList<InstanceFileState> expected = identifiers.Select(x =>
            new InstanceFileState
            {
                Version = x.VersionedInstanceIdentifier.Version,
                OriginalVersion = x.InstanceProperties.OriginalVersion,
                NewVersion = x.InstanceProperties.NewVersion
            }).ToList();

        var versions = expected.Select(x => x.Version).ToList();

        _indexStore.BeginUpdateInstancesAsync(Arg.Any<Partition>(), studyInstanceUid, CancellationToken.None).Returns(identifiers);

        IReadOnlyList<InstanceFileState> actual = await _updateDurableFunction.UpdateInstanceWatermarkAsync(
            new UpdateInstanceWatermarkArguments(Partition.DefaultKey, studyInstanceUid),
            NullLogger.Instance);

        await _indexStore
           .Received(1)
           .BeginUpdateInstancesAsync(Arg.Any<Partition>(), studyInstanceUid, cancellationToken: CancellationToken.None);

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
        IReadOnlyList<InstanceFileState> expected = identifiers.Select(x =>
            new InstanceFileState
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

        await _updateDurableFunction.UpdateInstanceBlobsAsync(
            new UpdateInstanceBlobArguments(Partition.DefaultKey, expected, dataset),
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

        _indexStore.EndUpdateInstanceAsync(Partition.DefaultKey, studyInstanceUid, new DicomDataset(), CancellationToken.None).Returns(Task.CompletedTask);

        var ds = new DicomDataset
        {
            { DicomTag.PatientName, "Patient Name" }
        };

        await _updateDurableFunction.CompleteUpdateStudyAsync(
            new CompleteStudyArguments(Partition.DefaultKey, studyInstanceUid, "{\"00100010\":{\"vr\":\"PN\",\"Value\":[{\"Alphabetic\":\"Patient Name\"}]}}"),
            NullLogger.Instance);

        await _indexStore
            .Received(1)
            .EndUpdateInstanceAsync(Partition.DefaultKey, studyInstanceUid, Arg.Is<DicomDataset>(x => x.GetSingleValue<string>(DicomTag.PatientName) == "Patient Name"), CancellationToken.None);
    }

    [Fact]
    public async Task GivenInstanceUpdateFails_WhenDeleteFile_ThenShouldDeleteSuccessfully()
    {
        var studyInstanceUid = TestUidGenerator.Generate();
        var identifiers = GetInstanceIdentifiersList(studyInstanceUid, instanceProperty: new InstanceProperties { NewVersion = 1 });
        IReadOnlyList<InstanceFileState> expected = identifiers.Select(x =>
            new InstanceFileState
            {
                Version = x.VersionedInstanceIdentifier.Version,
                OriginalVersion = x.InstanceProperties.OriginalVersion,
                NewVersion = x.InstanceProperties.NewVersion
            }).Take(1).ToList();

        // Arrange input
        IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
        context.GetInput<IReadOnlyList<InstanceFileState>>().Returns(expected);

        _updateInstanceService
            .DeleteInstanceBlobAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Call the activity
        await _updateDurableFunction.CleanupNewVersionBlobAsync(
            context,
            NullLogger.Instance);

        // Assert behavior
        context.Received(1).GetInput<IReadOnlyList<InstanceFileState>>();
        await _updateInstanceService
            .Received(1)
            .DeleteInstanceBlobAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task GivenInstanceMetadataList_WhenDeleteFile_ThenShouldDeleteSuccessfully()
    {
        var studyInstanceUid = TestUidGenerator.Generate();
        var identifiers = GetInstanceIdentifiersList(studyInstanceUid, instanceProperty: new InstanceProperties { OriginalVersion = 1 });
        IReadOnlyList<InstanceFileState> expected = identifiers.Select(x =>
            new InstanceFileState
            {
                Version = x.VersionedInstanceIdentifier.Version,
                OriginalVersion = x.InstanceProperties.OriginalVersion,
                NewVersion = x.InstanceProperties.NewVersion
            }).Take(1).ToList();

        // Arrange input
        IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
        context.GetInput<IReadOnlyList<InstanceFileState>>().Returns(expected);

        _updateInstanceService
            .DeleteInstanceBlobAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Call the activity
        await _updateDurableFunction.DeleteOldVersionBlobAsync(
            context,
            NullLogger.Instance);

        // Assert behavior
        context.Received(1).GetInput<IReadOnlyList<InstanceFileState>>();
        await _updateInstanceService
            .Received(1)
            .DeleteInstanceBlobAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }


    private static List<InstanceMetadata> GetInstanceIdentifiersList(string studyInstanceUid, Partition partition = null, InstanceProperties instanceProperty = null)
    {
        var dicomInstanceIdentifiersList = new List<InstanceMetadata>();
        instanceProperty ??= new InstanceProperties();
        partition ??= Partition.Default;

        instanceProperty = instanceProperty ?? new InstanceProperties();

        dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate(), 0, partition), instanceProperty));
        dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1, partition), instanceProperty));
        return dicomInstanceIdentifiersList;
    }
}
