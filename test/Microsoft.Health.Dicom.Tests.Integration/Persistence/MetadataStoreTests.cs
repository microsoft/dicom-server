// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class MetadataStoreTests : IClassFixture<DataStoreTestsFixture>
    {
        private readonly IMetadataStore _metadataStore;

        public MetadataStoreTests(DataStoreTestsFixture fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            _metadataStore = fixture.MetadataStore;
        }

        [Fact]
        public async Task GivenAValidInstanceMetadata_WhenStored_ThenItCanBeRetrievedAndDeleted()
        {
            DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset();
            var instanceIdentifier = dicomDataset.ToVersionedInstanceIdentifier(version: 0);

            // Store the metadata.
            await _metadataStore.StoreInstanceMetadataAsync(dicomDataset, 0);

            // Should be able to retrieve.
            DicomDataset retrievedDicomDataset = await _metadataStore.GetInstanceMetadataAsync(instanceIdentifier);

            ValidateDicomDataset(dicomDataset, retrievedDicomDataset);

            // Should be able to delete.
            await _metadataStore.DeleteInstanceMetadataIfExistsAsync(instanceIdentifier);

            // The file should no longer exists.
            await Assert.ThrowsAsync<ItemNotFoundException>(() => _metadataStore.GetInstanceMetadataAsync(instanceIdentifier));
        }

        [Fact]
        public async Task GivenMetadataAlreadyExists_WhenStored_ThenExistingMetadataWillBeOverwritten()
        {
            DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset();

            dicomDataset.AddOrUpdate(DicomTag.SOPClassUID, "1");

            await _metadataStore.StoreInstanceMetadataAsync(dicomDataset, 0);

            // Update SOPClassUID but keep the identifiers the same.
            dicomDataset.AddOrUpdate(DicomTag.SOPClassUID, "2");

            await _metadataStore.StoreInstanceMetadataAsync(dicomDataset, 0);

            // Should be able to retrieve.
            DicomDataset retrievedDicomDataset = await _metadataStore.GetInstanceMetadataAsync(
                dicomDataset.ToVersionedInstanceIdentifier(0));

            ValidateDicomDataset(dicomDataset, retrievedDicomDataset);

            Assert.Equal("2", retrievedDicomDataset.GetSingleValue<string>(DicomTag.SOPClassUID));
        }

        [Fact]
        public async Task GivenANonExistingMetadata_WhenRetrievingInstanceMetadata_ThenItemNotFoundExceptionShouldBeThrown()
        {
            var instanceIdentifier = new VersionedInstanceIdentifier(
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: TestUidGenerator.Generate(),
                sopInstanceUid: TestUidGenerator.Generate(),
                version: 0);

            await Assert.ThrowsAsync<ItemNotFoundException>(
                () => _metadataStore.GetInstanceMetadataAsync(instanceIdentifier));
        }

        [Fact]
        public async Task GivenANonExistenMetadata_WhenDeleting_ThenItShouldNotThrowException()
        {
            DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset();
            var instanceIdentifier = dicomDataset.ToVersionedInstanceIdentifier(version: 0);

            await _metadataStore.StoreInstanceMetadataAsync(dicomDataset, version: 0);

            await _metadataStore.DeleteInstanceMetadataIfExistsAsync(instanceIdentifier);

            await _metadataStore.DeleteInstanceMetadataIfExistsAsync(instanceIdentifier);
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
}
