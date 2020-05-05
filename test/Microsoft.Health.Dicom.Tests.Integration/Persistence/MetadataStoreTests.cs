// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Dicom;
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
            _metadataStore = fixture.MetadataStore;
        }

        [Fact]
        public async Task GivenInvalidParameters_WhenAddingInstanceMetadata_ArgumentExceptionIsThrown()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _metadataStore.AddInstanceMetadataAsync(null, 0));
        }

        [Fact]
        public async Task GivenAnUnknownDicomInstance_WhenFetchingInstanceMetadata_NotFoundDataStoreExceptionIsThrown()
        {
            var dicomInstanceId = new VersionedInstanceIdentifier(
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: TestUidGenerator.Generate(),
                sopInstanceUid: TestUidGenerator.Generate(),
                version: 0);
            await Assert.ThrowsAsync<InstanceNotFoundException>(
                () => _metadataStore.GetInstanceMetadataAsync(dicomInstanceId));
        }

        [Fact]
        public async Task GivenADeletedDicomInstance_WhenFetchingInstanceMetadata_NotFoundDataStoreExceptionIsThrown()
        {
            DicomDataset dicomDataset = CreateValidMetadataDataset();
            var dicomInstanceId = dicomDataset.ToVersionedDicomInstanceIdentifier(version: 0);

            await _metadataStore.AddInstanceMetadataAsync(dicomDataset, version: 0);
            DicomDataset storedMetadata = await _metadataStore.GetInstanceMetadataAsync(dicomInstanceId);
            Assert.NotNull(storedMetadata);

            await _metadataStore.DeleteInstanceMetadataIfExistsAsync(dicomInstanceId);

            await Assert.ThrowsAsync<InstanceNotFoundException>(
                () => _metadataStore.GetInstanceMetadataAsync(dicomInstanceId));
        }

        [Fact]
        public async Task GivenExistingMetadata_WhenAdding_ConflictExceptionIsThrown()
        {
            DicomDataset dicomDataset = CreateValidMetadataDataset();
            var dicomInstanceId = dicomDataset.ToVersionedDicomInstanceIdentifier(version: 0);
            await _metadataStore.AddInstanceMetadataAsync(dicomDataset, version: 0);
            DicomDataset storedMetadata = await _metadataStore.GetInstanceMetadataAsync(dicomInstanceId);
            Assert.NotNull(storedMetadata);

            InstanceAlreadyExistsException exception = await Assert.ThrowsAsync<InstanceAlreadyExistsException>(
                () => _metadataStore.AddInstanceMetadataAsync(dicomDataset, version: 0));

            await _metadataStore.DeleteInstanceMetadataIfExistsAsync(dicomInstanceId);
        }

        [Fact]
        public async Task GivenAddedInstanceMetadata_WhenDeletingAgain_NoExceptionIsThrown()
        {
            DicomDataset dicomDataset = CreateValidMetadataDataset();
            var dicomInstanceId = dicomDataset.ToVersionedDicomInstanceIdentifier(version: 0);

            await _metadataStore.AddInstanceMetadataAsync(dicomDataset, version: 0);
            DicomDataset storedMetadata = await _metadataStore.GetInstanceMetadataAsync(dicomInstanceId);
            Assert.NotNull(storedMetadata);

            await _metadataStore.DeleteInstanceMetadataIfExistsAsync(dicomInstanceId);

            await _metadataStore.DeleteInstanceMetadataIfExistsAsync(dicomInstanceId);
        }

        private DicomDataset CreateValidMetadataDataset()
        {
            return new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.SeriesInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.SOPInstanceUID, TestUidGenerator.Generate() },
            };
        }
    }
}
