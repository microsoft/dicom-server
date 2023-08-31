// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Functions.Update.Models;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Update;

public partial class UpdateDurableFunctionTests
{
    private static readonly FileProperties DefaultFileProperties = new FileProperties
    {
        Path = "default/path/0.dcm",
        ETag = "123"
    };

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

        IEnumerable<InstanceMetadata> result = await _updateDurableFunction.UpdateInstanceWatermarkV2Async(
            new UpdateInstanceWatermarkArgumentsV2(Partition.Default, studyInstanceUid),
            NullLogger.Instance);
        IReadOnlyList<InstanceMetadata> actual = result.ToList();

        await _indexStore
           .Received(1)
           .BeginUpdateInstancesAsync(Arg.Any<Partition>(), studyInstanceUid, cancellationToken: CancellationToken.None);

        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Version, actual[i].VersionedInstanceIdentifier.Version);
            Assert.Equal(expected[i].OriginalVersion, actual[i].InstanceProperties.OriginalVersion);
            Assert.Equal(expected[i].NewVersion, actual[i].InstanceProperties.NewVersion);
        }
    }

    [Fact]
    public async Task GivenInstanceMetadata_WhenUpdatingBlobInBatches_ThenShouldUpdateCorrectly()
    {
        var studyInstanceUid = TestUidGenerator.Generate();
        var expected = GetInstanceIdentifiersList(studyInstanceUid);

        var dataset = "{\"00100010\":{\"vr\":\"PN\",\"Value\":[{\"Alphabetic\":\"Patient Name\"}]}}";

        foreach (var instance in expected)
        {
            _updateInstanceService
                .UpdateInstanceBlobAsync(
                    instance,
                    Arg.Is<DicomDataset>(x => x.GetSingleValue<string>(DicomTag.PatientName) == "Patient Name"),
                    Partition.Default,
                    Arg.Any<CancellationToken>())
                .Returns(DefaultFileProperties);
        }

        await _updateDurableFunction.UpdateInstanceBlobsV2Async(
            new UpdateInstanceBlobArgumentsV2(Partition.Default, expected, dataset),
            NullLogger.Instance);

        foreach (var instance in expected)
        {
            await _updateInstanceService
                .Received(1)
                .UpdateInstanceBlobAsync(Arg.Is(GetPredicate(instance)), Arg.Is<DicomDataset>(x => x.GetSingleValue<string>(DicomTag.PatientName) == "Patient Name"),
                Partition.Default,
                Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task GivenCompleteInstanceArgument_WhenCompleting_ThenShouldComplete()
    {
        var studyInstanceUid = TestUidGenerator.Generate();

        var instanceMetadataList = new List<InstanceMetadata>();
        _indexStore.EndUpdateInstanceAsync(Partition.DefaultKey, studyInstanceUid, new DicomDataset(), instanceMetadataList, CancellationToken.None).Returns(Task.CompletedTask);

        var ds = new DicomDataset
        {
            { DicomTag.PatientName, "Patient Name" }
        };

        await _updateDurableFunction.CompleteUpdateStudyV2Async(
            new CompleteStudyArgumentsV2(
                Partition.DefaultKey,
                studyInstanceUid,
                "{\"00100010\":{\"vr\":\"PN\",\"Value\":[{\"Alphabetic\":\"Patient Name\"}]}}",
                instanceMetadataList),
            NullLogger.Instance);

        await _indexStore
            .Received(1)
            .EndUpdateInstanceAsync(Partition.DefaultKey, studyInstanceUid, Arg.Is<DicomDataset>(x => x.GetSingleValue<string>(DicomTag.PatientName) == "Patient Name"), instanceMetadataList, CancellationToken.None);
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

        _updateInstanceService
            .DeleteInstanceBlobAsync(Arg.Any<long>(), Partition.Default, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Call the activity
        await _updateDurableFunction.CleanupNewVersionBlobV2Async(
            new CleanupBlobArguments(expected, Partition.Default),
            NullLogger.Instance);

        // Assert behavior
        await _updateInstanceService
            .Received(1)
            .DeleteInstanceBlobAsync(Arg.Any<long>(), Partition.Default, Arg.Any<CancellationToken>());
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

        _updateInstanceService
            .DeleteInstanceBlobAsync(Arg.Any<long>(), Partition.Default, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Call the activity
        await _updateDurableFunction.DeleteOldVersionBlobV2Async(
            new CleanupBlobArguments(expected, Partition.Default),
            NullLogger.Instance);

        // Assert behavior
        await _updateInstanceService
            .Received(1)
            .DeleteInstanceBlobAsync(Arg.Any<long>(), Partition.Default, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenInstanceMetadataList_WhenChangeAccessTier_ThenShoulChangeSuccessfully()
    {
        var studyInstanceUid = TestUidGenerator.Generate();
        var identifiers = GetInstanceIdentifiersList(studyInstanceUid, Partition.Default, new InstanceProperties { NewVersion = 2 });
        IReadOnlyList<InstanceFileState> expected = identifiers.Select(x =>
            new InstanceFileState
            {
                Version = x.VersionedInstanceIdentifier.Version,
                OriginalVersion = x.InstanceProperties.OriginalVersion,
                NewVersion = x.InstanceProperties.NewVersion
            }).Take(1).ToList();

        _fileStore
            .SetBlobToColdAccessTierAsync(Arg.Any<long>(), Partition.Default, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Call the activity
        await _updateDurableFunction.SetOriginalBlobToColdAccessTierAsync(
            new CleanupBlobArguments(expected, Partition.Default),
            NullLogger.Instance);

        // Assert behavior
        await _fileStore
            .Received(1)
            .SetBlobToColdAccessTierAsync(Arg.Any<long>(), Partition.Default, Arg.Any<CancellationToken>());
    }


    private static List<InstanceMetadata> GetInstanceIdentifiersList(string studyInstanceUid, Partition partition = null, InstanceProperties instanceProperty = null)
    {
        var dicomInstanceIdentifiersList = new List<InstanceMetadata>();
        instanceProperty ??= new InstanceProperties { NewVersion = 2, OriginalVersion = 3 };
        partition ??= Partition.Default;

        dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate(), 0, partition), instanceProperty));
        dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1, partition), instanceProperty));
        return dicomInstanceIdentifiersList;
    }
}
