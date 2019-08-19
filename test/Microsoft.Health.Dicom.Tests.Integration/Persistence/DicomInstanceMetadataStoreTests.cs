// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DicomInstanceMetadataStoreTests : IClassFixture<DicomBlobStorageTestsFixture>
    {
        private readonly IDicomInstanceMetadataStore _dicomInstanceMetadataStore;

        public DicomInstanceMetadataStoreTests(DicomBlobStorageTestsFixture fixture)
        {
            _dicomInstanceMetadataStore = fixture.DicomInstanceMetadataStore;
        }

        [Fact]
        public async Task GivenInvalidParameters_WhenAddingDeletingFetchingInstanceMetadata_ArgumentExceptionIsThrown()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomInstanceMetadataStore.AddInstanceMetadataAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomInstanceMetadataStore.AddInstanceMetadataAsync(new DicomDataset()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomInstanceMetadataStore.AddInstanceMetadataAsync(new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, Guid.NewGuid().ToString() },
            }));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomInstanceMetadataStore.AddInstanceMetadataAsync(new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, Guid.NewGuid().ToString() },
                { DicomTag.SeriesInstanceUID, Guid.NewGuid().ToString() },
            }));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomInstanceMetadataStore.DeleteInstanceMetadataAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomInstanceMetadataStore.GetInstanceMetadataAsync(null));
        }

        [Fact]
        public async Task GivenAnUnknownDicomInstance_WhenFetchingInstanceMetadata_NotFoundDataStoreExceptionIsThrown()
        {
            var dicomInstance = new DicomInstance(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            DataStoreException exception = await Assert.ThrowsAsync<DataStoreException>(
                () => _dicomInstanceMetadataStore.GetInstanceMetadataAsync(dicomInstance));
            Assert.Equal((int)HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenAnUnknownDicomInstance_WhenDeletingInstanceMetadata_NotFoundDataStoreExceptionIsThrown()
        {
            var dicomInstance = new DicomInstance(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            DataStoreException exception = await Assert.ThrowsAsync<DataStoreException>(
                () => _dicomInstanceMetadataStore.DeleteInstanceMetadataAsync(dicomInstance));
            Assert.Equal((int)HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenExistingMetadata_WhenAdding_ConflictExceptionIsThrown()
        {
            DicomDataset dicomDataset = CreateValidMetadataDataset();
            var dicomInstance = DicomInstance.Create(dicomDataset);

            await _dicomInstanceMetadataStore.AddInstanceMetadataAsync(dicomDataset);
            DicomDataset storedMetadata = await _dicomInstanceMetadataStore.GetInstanceMetadataAsync(dicomInstance);
            Assert.NotNull(storedMetadata);

            DataStoreException exception = await Assert.ThrowsAsync<DataStoreException>(
                () => _dicomInstanceMetadataStore.AddInstanceMetadataAsync(dicomDataset));
            Assert.Equal((int)HttpStatusCode.Conflict, exception.StatusCode);

            await _dicomInstanceMetadataStore.DeleteInstanceMetadataAsync(dicomInstance);

            exception = await Assert.ThrowsAsync<DataStoreException>(
                () => _dicomInstanceMetadataStore.DeleteInstanceMetadataAsync(dicomInstance));
            Assert.Equal((int)HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenAddedInstanceMetadata_WhenDeletingAgain_NotFoundExceptionIsThrown()
        {
            DicomDataset dicomDataset = CreateValidMetadataDataset();
            var dicomInstance = DicomInstance.Create(dicomDataset);

            await _dicomInstanceMetadataStore.AddInstanceMetadataAsync(dicomDataset);
            DicomDataset storedMetadata = await _dicomInstanceMetadataStore.GetInstanceMetadataAsync(dicomInstance);
            Assert.NotNull(storedMetadata);

            await _dicomInstanceMetadataStore.DeleteInstanceMetadataAsync(dicomInstance);

            DataStoreException exception = await Assert.ThrowsAsync<DataStoreException>(
                () => _dicomInstanceMetadataStore.DeleteInstanceMetadataAsync(dicomInstance));
            Assert.Equal((int)HttpStatusCode.NotFound, exception.StatusCode);
        }

        private DicomDataset CreateValidMetadataDataset()
        {
            return new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, Guid.NewGuid().ToString() },
                { DicomTag.SeriesInstanceUID, Guid.NewGuid().ToString() },
                { DicomTag.SOPInstanceUID, Guid.NewGuid().ToString() },
            };
        }
    }
}
