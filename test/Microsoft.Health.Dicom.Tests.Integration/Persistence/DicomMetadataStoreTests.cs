// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DicomMetadataStoreTests : IClassFixture<DicomDataStoreTestsFixture>
    {
        private readonly IDicomMetadataStore _dicomMetadataStore;

        public DicomMetadataStoreTests(DicomDataStoreTestsFixture fixture)
        {
            _dicomMetadataStore = fixture.DicomMetadataStore;
        }

        [Fact]
        public async Task GivenInvalidParameters_WhenAddingInstanceMetadata_ArgumentExceptionIsThrown()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.AddInstanceMetadataAsync(null, 0));
        }

        [Fact]
        public async Task GivenAnUnknownDicomInstance_WhenFetchingInstanceMetadata_NotFoundDataStoreExceptionIsThrown()
        {
            var dicomInstanceId = new VersionedDicomInstanceIdentifier(
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: TestUidGenerator.Generate(),
                sopInstanceUid: TestUidGenerator.Generate(),
                version: 0);
            DicomDataStoreException exception = await Assert.ThrowsAsync<DicomDataStoreException>(
                () => _dicomMetadataStore.GetInstanceMetadataAsync(dicomInstanceId));
            Assert.Equal((int)HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenExistingMetadata_WhenAdding_ConflictExceptionIsThrown()
        {
            DicomDataset dicomDataset = CreateValidMetadataDataset();
            var dicomInstanceId = dicomDataset.ToVersionedDicomInstanceIdentifier(version: 0);
            await _dicomMetadataStore.AddInstanceMetadataAsync(dicomDataset, version: 0);
            DicomDataset storedMetadata = await _dicomMetadataStore.GetInstanceMetadataAsync(dicomInstanceId);
            Assert.NotNull(storedMetadata);

            DicomDataStoreException exception = await Assert.ThrowsAsync<DicomDataStoreException>(
                () => _dicomMetadataStore.AddInstanceMetadataAsync(dicomDataset, version: 0));
            Assert.Equal((int)HttpStatusCode.Conflict, exception.StatusCode);

            await _dicomMetadataStore.DeleteInstanceMetadataIfExistsAsync(dicomInstanceId);
        }

        [Fact]
        public async Task GivenAddedInstanceMetadata_WhenDeletingAgain_NoExceptionIsThrown()
        {
            DicomDataset dicomDataset = CreateValidMetadataDataset();
            var dicomInstanceId = dicomDataset.ToVersionedDicomInstanceIdentifier(version: 0);

            await _dicomMetadataStore.AddInstanceMetadataAsync(dicomDataset, version: 0);
            DicomDataset storedMetadata = await _dicomMetadataStore.GetInstanceMetadataAsync(dicomInstanceId);
            Assert.NotNull(storedMetadata);

            await _dicomMetadataStore.DeleteInstanceMetadataIfExistsAsync(dicomInstanceId);

            await _dicomMetadataStore.DeleteInstanceMetadataIfExistsAsync(dicomInstanceId);
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
