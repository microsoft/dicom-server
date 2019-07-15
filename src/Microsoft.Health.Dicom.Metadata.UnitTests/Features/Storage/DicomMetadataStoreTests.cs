// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Metadata.Config;
using Microsoft.Health.Dicom.Metadata.Features.Storage;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Metadata.UnitTests.Features.Storage
{
    public class DicomMetadataStoreTests
    {
        private readonly DicomMetadataStore _dicomMetadataStore;

        public DicomMetadataStoreTests()
        {
            IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor = Substitute.For<IOptionsMonitor<BlobContainerConfiguration>>();
            namedBlobContainerConfigurationAccessor.Get(Constants.ContainerConfigurationName).Returns(new BlobContainerConfiguration() { ContainerName = "testcontainer" });
            _dicomMetadataStore = new DicomMetadataStore(
                Substitute.For<CloudBlobClient>(new Uri("https://www.microsoft.com/"), null),
                namedBlobContainerConfigurationAccessor,
                new DicomMetadataConfiguration(),
                NullLogger<DicomMetadataStore>.Instance);
        }

        [Fact]
        public async Task GivenInvalidArguments_WhenAddingInstanceMetadata_ArgumentExceptionIsThrown()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.AddStudySeriesDicomMetadataAsync((DicomDataset)null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.AddStudySeriesDicomMetadataAsync((IEnumerable<DicomDataset>)null));

            // Missing Study Instance UID
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(new DicomDataset()
            {
                { DicomTag.SeriesInstanceUID, Guid.NewGuid().ToString() },
                { DicomTag.SOPInstanceUID, Guid.NewGuid().ToString() },
            }));

            // Missing Series Instance UID
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, Guid.NewGuid().ToString() },
                { DicomTag.SOPInstanceUID, Guid.NewGuid().ToString() },
            }));

            // Missing SOP Instance UID
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, Guid.NewGuid().ToString() },
                { DicomTag.SeriesInstanceUID, Guid.NewGuid().ToString() },
            }));
        }

        [Fact]
        public async Task GivenInvalidArguments_WhenFetchingInstancesOrMetadata_ArgumentExceptionIsThrown()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetInstancesInStudyAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetInstancesInStudyAsync(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetInstancesInStudyAsync(new string('a', 65)));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetStudyDicomMetadataWithAllOptionalAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetStudyDicomMetadataWithAllOptionalAsync(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetStudyDicomMetadataWithAllOptionalAsync(new string('a', 65)));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetStudyDicomMetadataAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetStudyDicomMetadataAsync(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetStudyDicomMetadataAsync(new string('a', 65)));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetInstancesInSeriesAsync(null, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetInstancesInSeriesAsync(string.Empty, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetInstancesInSeriesAsync(new string('a', 65), Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetInstancesInSeriesAsync(Guid.NewGuid().ToString(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetInstancesInSeriesAsync(Guid.NewGuid().ToString(), string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetInstancesInSeriesAsync(Guid.NewGuid().ToString(), new string('a', 65)));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(null, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(string.Empty, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(new string('a', 65), Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(Guid.NewGuid().ToString(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(Guid.NewGuid().ToString(), string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(Guid.NewGuid().ToString(), new string('a', 65)));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetSeriesDicomMetadataAsync(null, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataAsync(string.Empty, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataAsync(new string('a', 65), Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetSeriesDicomMetadataAsync(Guid.NewGuid().ToString(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataAsync(Guid.NewGuid().ToString(), string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataAsync(Guid.NewGuid().ToString(), new string('a', 65)));
        }

        [Fact]
        public async Task GivenInvalidArguments_WhenDeletingMetadata_ArgumentExceptionIsThrown()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.DeleteStudyAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteStudyAsync(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteStudyAsync(new string('a', 65)));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.DeleteSeriesAsync(null, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteSeriesAsync(string.Empty, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteSeriesAsync(new string('a', 65), Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.DeleteSeriesAsync(Guid.NewGuid().ToString(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteSeriesAsync(Guid.NewGuid().ToString(), string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteSeriesAsync(Guid.NewGuid().ToString(), new string('a', 65)));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.DeleteInstanceAsync(null, Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteInstanceAsync(string.Empty, Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteInstanceAsync(new string('a', 65), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.DeleteInstanceAsync(Guid.NewGuid().ToString(), null, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteInstanceAsync(Guid.NewGuid().ToString(), string.Empty, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteInstanceAsync(Guid.NewGuid().ToString(), new string('a', 65), Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.DeleteInstanceAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteInstanceAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteInstanceAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new string('a', 65)));
        }
    }
}
