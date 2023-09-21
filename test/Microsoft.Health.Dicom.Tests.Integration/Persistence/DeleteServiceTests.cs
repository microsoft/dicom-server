// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence;

public class DeleteServiceTests : IClassFixture<DeleteServiceTestsFixture>
{
    private readonly DeleteServiceTestsFixture _fixture;

    public DeleteServiceTests(DeleteServiceTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task GivenDeletedInstance_WhenCleanupCalledWithDifferentStorePersistence_FilesAndTriesAreRemoved(bool persistBlob, bool persistMetadata)
    {
        (var dicomInstanceIdentifier, var fileProperties) = await CreateAndValidateValuesInStores(persistBlob, persistMetadata);
        await DeleteAndValidateInstanceForCleanup(dicomInstanceIdentifier);

        await Task.Delay(3000, CancellationToken.None);
        (bool success, int retrievedInstanceCount) = await _fixture.DeleteService.CleanupDeletedInstancesAsync(CancellationToken.None);

        await ValidateRemoval(success, retrievedInstanceCount, dicomInstanceIdentifier, fileProperties: fileProperties);
    }

    private async Task DeleteAndValidateInstanceForCleanup(VersionedInstanceIdentifier versionedInstanceIdentifier)
    {
        await _fixture.DeleteService.DeleteInstanceAsync(versionedInstanceIdentifier.StudyInstanceUid, versionedInstanceIdentifier.SeriesInstanceUid, versionedInstanceIdentifier.SopInstanceUid, CancellationToken.None);

        Assert.NotEmpty(await _fixture.IndexDataStoreTestHelper.GetDeletedInstanceEntriesAsync(versionedInstanceIdentifier.StudyInstanceUid, versionedInstanceIdentifier.SeriesInstanceUid, versionedInstanceIdentifier.SopInstanceUid));
    }

    private async Task<(VersionedInstanceIdentifier, FileProperties)> CreateAndValidateValuesInStores(bool persistBlob, bool persistMetadata)
    {
        var newDataSet = CreateValidMetadataDataset();

        var version = await _fixture.IndexDataStore.BeginCreateInstanceIndexAsync(Partition.Default, newDataSet);
        var versionedDicomInstanceIdentifier = newDataSet.ToVersionedInstanceIdentifier(version, Partition.Default);

        if (persistMetadata)
        {
            await _fixture.MetadataStore.StoreInstanceMetadataAsync(newDataSet, version);

            var metaEntry = await _fixture.MetadataStore.GetInstanceMetadataAsync(version);
            Assert.Equal(versionedDicomInstanceIdentifier.SopInstanceUid, metaEntry.GetSingleValue<string>(DicomTag.SOPInstanceUID));
        }

        if (persistBlob)
        {
            var fileData = new byte[] { 4, 7, 2 };

            await using (MemoryStream stream = _fixture.RecyclableMemoryStreamManager.GetStream("GivenDeletedInstances_WhenCleanupCalled_FilesAndTriesAreRemoved.fileData", fileData, 0, fileData.Length))
            {
                FileProperties fileProperties = await _fixture.FileStore.StoreFileAsync(version, Partition.DefaultName, stream);

                Assert.NotNull(fileProperties);

                await _fixture.IndexDataStore.EndCreateInstanceIndexAsync(1, newDataSet, version, fileProperties);

                // ensure properties were saved
                Assert.NotEmpty(await _fixture.IndexDataStoreTestHelper.GetFilePropertiesAsync(version));

                var file = await _fixture.FileStore.GetFileAsync(version, Partition.Default, fileProperties);

                Assert.NotNull(file);

                return (versionedDicomInstanceIdentifier, fileProperties);
            }
        }

        return (versionedDicomInstanceIdentifier, null);
    }

    private async Task ValidateRemoval(bool success, int retrievedInstanceCount, VersionedInstanceIdentifier versionedInstanceIdentifier, bool persistBlob = false, FileProperties fileProperties = null)
    {
        Assert.True(success);
        Assert.Equal(1, retrievedInstanceCount);

        fileProperties ??= new FileProperties();

        await Assert.ThrowsAsync<ItemNotFoundException>(() => _fixture.MetadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifier.Version));
        await Assert.ThrowsAsync<ItemNotFoundException>(() => _fixture.FileStore.GetFileAsync(versionedInstanceIdentifier.Version, versionedInstanceIdentifier.Partition, fileProperties));

        Assert.Empty(await _fixture.IndexDataStoreTestHelper.GetDeletedInstanceEntriesAsync(versionedInstanceIdentifier.StudyInstanceUid, versionedInstanceIdentifier.SeriesInstanceUid, versionedInstanceIdentifier.SopInstanceUid));
        if (persistBlob)
        {
            // ensure properties were deleted
            IReadOnlyList<FileProperty> retrievedFleProperties = await _fixture.IndexDataStoreTestHelper
                .GetFilePropertiesAsync(versionedInstanceIdentifier.Version);
            Assert.Empty(retrievedFleProperties);
        }
    }

    private static DicomDataset CreateValidMetadataDataset()
    {
        return new DicomDataset()
        {
            { DicomTag.StudyInstanceUID, TestUidGenerator.Generate() },
            { DicomTag.SeriesInstanceUID, TestUidGenerator.Generate() },
            { DicomTag.SOPInstanceUID, TestUidGenerator.Generate() },
            { DicomTag.PatientID, TestUidGenerator.Generate() },
        };
    }
}
