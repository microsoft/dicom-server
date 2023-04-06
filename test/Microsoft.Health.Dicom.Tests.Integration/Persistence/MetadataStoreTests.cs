// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence;

public class MetadataStoreTests : IClassFixture<DataStoreTestsFixture>
{
    private readonly IMetadataStore _metadataStore;
    private readonly Func<int> _getNextWatermark;

    public MetadataStoreTests(DataStoreTestsFixture fixture)
    {
        EnsureArg.IsNotNull(fixture, nameof(fixture));
        _metadataStore = fixture.MetadataStore;
        _getNextWatermark = () => fixture.NextWatermark;
    }

    [Fact]
    public async Task GivenAValidInstanceMetadata_WhenStored_ThenItCanBeRetrievedAndDeleted()
    {
        DicomDataset dicomDataset = CreateValidMetadataDataset();
        int version = _getNextWatermark();

        // Store the metadata.
        await _metadataStore.StoreInstanceMetadataAsync(dicomDataset, version);

        // Should be able to retrieve.
        DicomDataset retrievedDicomDataset = await _metadataStore.GetInstanceMetadataAsync(version);

        ValidateDicomDataset(dicomDataset, retrievedDicomDataset);

        // Should be able to delete.
        await _metadataStore.DeleteInstanceMetadataIfExistsAsync(version);

        // The file should no longer exists.
        await Assert.ThrowsAsync<ItemNotFoundException>(() => _metadataStore.GetInstanceMetadataAsync(version));
    }

    [Fact]
    public async Task GivenMetadataAlreadyExists_WhenStored_ThenExistingMetadataWillBeOverwritten()
    {
        DicomDataset dicomDataset = CreateValidMetadataDataset();
        dicomDataset.Add(DicomTag.SOPClassUID, "1");
        int version = _getNextWatermark();

        await _metadataStore.StoreInstanceMetadataAsync(dicomDataset, version);

        // Update SOPClassUID but keep the identifiers the same.
        dicomDataset.AddOrUpdate(DicomTag.SOPClassUID, "2");

        await _metadataStore.StoreInstanceMetadataAsync(dicomDataset, version);

        // Should be able to retrieve.
        DicomDataset retrievedDicomDataset = await _metadataStore.GetInstanceMetadataAsync(version);

        ValidateDicomDataset(dicomDataset, retrievedDicomDataset);

        Assert.Equal("2", retrievedDicomDataset.GetSingleValue<string>(DicomTag.SOPClassUID));
    }

    [Fact]
    public async Task GivenANonExistingMetadata_WhenRetrievingInstanceMetadata_ThenItemNotFoundExceptionShouldBeThrown()
    {
        int version = _getNextWatermark();

        await Assert.ThrowsAsync<ItemNotFoundException>(
            () => _metadataStore.GetInstanceMetadataAsync(version));
    }

    [Fact]
    public async Task GivenANonExistenMetadata_WhenDeleting_ThenItShouldNotThrowException()
    {
        DicomDataset dicomDataset = CreateValidMetadataDataset();
        int version = _getNextWatermark();

        await _metadataStore.StoreInstanceMetadataAsync(dicomDataset, version);

        await _metadataStore.DeleteInstanceMetadataIfExistsAsync(version);

        await _metadataStore.DeleteInstanceMetadataIfExistsAsync(version);
    }

    private static DicomDataset CreateValidMetadataDataset()
    {
        return new DicomDataset()
        {
            { DicomTag.StudyInstanceUID, TestUidGenerator.Generate() },
            { DicomTag.SeriesInstanceUID, TestUidGenerator.Generate() },
            { DicomTag.SOPInstanceUID, TestUidGenerator.Generate() },
        };
    }

    private static void ValidateDicomDataset(DicomDataset expectedDicomDataset, DicomDataset actualDicomDataset)
    {
        Assert.NotNull(actualDicomDataset);

        ValidateAttribute(DicomTag.StudyInstanceUID);
        ValidateAttribute(DicomTag.SeriesInstanceUID);
        ValidateAttribute(DicomTag.SOPInstanceUID);

        void ValidateAttribute(DicomTag dicomTag)
        {
            Assert.Equal(
                expectedDicomDataset.GetSingleValue<string>(dicomTag),
                actualDicomDataset.GetSingleValue<string>(dicomTag));
        }
    }
}
