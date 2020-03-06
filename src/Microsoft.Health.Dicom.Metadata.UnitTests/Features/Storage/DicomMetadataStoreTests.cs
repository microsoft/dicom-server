// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Metadata.Config;
using Microsoft.Health.Dicom.Metadata.Features.Storage;
using Microsoft.Health.Dicom.Tests.Common;
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
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(null));

            // Missing Study Instance UID
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(
                CreateRandomInstanceDataset(studyInstanceUID: null, seriesInstanceUID: TestUidGenerator.Generate(), sopInstanceUID: TestUidGenerator.Generate())));

            // Missing Series Instance UID
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(
                CreateRandomInstanceDataset(studyInstanceUID: TestUidGenerator.Generate(), seriesInstanceUID: null, sopInstanceUID: TestUidGenerator.Generate())));

            // Missing SOP Instance UID
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(
                CreateRandomInstanceDataset(studyInstanceUID: TestUidGenerator.Generate(), seriesInstanceUID: TestUidGenerator.Generate(), sopInstanceUID: null)));

            // Different Study Instance UIDs
            await Assert.ThrowsAsync<ArgumentException>(
                () => _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(
                    new[]
                    {
                        CreateRandomInstanceDataset(studyInstanceUID: TestUidGenerator.Generate(), seriesInstanceUID: TestUidGenerator.Generate(), sopInstanceUID: TestUidGenerator.Generate()),
                        CreateRandomInstanceDataset(studyInstanceUID: TestUidGenerator.Generate(), seriesInstanceUID: TestUidGenerator.Generate(), sopInstanceUID: TestUidGenerator.Generate()),
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

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetInstancesInSeriesAsync(null, TestUidGenerator.Generate()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetInstancesInSeriesAsync(string.Empty, TestUidGenerator.Generate()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetInstancesInSeriesAsync(new string('a', 65), TestUidGenerator.Generate()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetInstancesInSeriesAsync(TestUidGenerator.Generate(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetInstancesInSeriesAsync(TestUidGenerator.Generate(), string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetInstancesInSeriesAsync(TestUidGenerator.Generate(), new string('a', 65)));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(null, TestUidGenerator.Generate()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(string.Empty, TestUidGenerator.Generate()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(new string('a', 65), TestUidGenerator.Generate()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(TestUidGenerator.Generate(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(TestUidGenerator.Generate(), string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(TestUidGenerator.Generate(), new string('a', 65)));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetSeriesDicomMetadataAsync(null, TestUidGenerator.Generate()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataAsync(string.Empty, TestUidGenerator.Generate()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataAsync(new string('a', 65), TestUidGenerator.Generate()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetSeriesDicomMetadataAsync(TestUidGenerator.Generate(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataAsync(TestUidGenerator.Generate(), string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataAsync(TestUidGenerator.Generate(), new string('a', 65)));
        }

        [Fact]
        public async Task GivenInvalidArguments_WhenDeletingMetadata_ArgumentExceptionIsThrown()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.DeleteStudyAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteStudyAsync(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteStudyAsync(new string('a', 65)));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.DeleteSeriesAsync(null, TestUidGenerator.Generate()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteSeriesAsync(string.Empty, TestUidGenerator.Generate()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteSeriesAsync(new string('a', 65), TestUidGenerator.Generate()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.DeleteSeriesAsync(TestUidGenerator.Generate(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteSeriesAsync(TestUidGenerator.Generate(), string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteSeriesAsync(TestUidGenerator.Generate(), new string('a', 65)));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.DeleteInstanceAsync(null, TestUidGenerator.Generate(), TestUidGenerator.Generate()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteInstanceAsync(string.Empty, TestUidGenerator.Generate(), TestUidGenerator.Generate()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteInstanceAsync(new string('a', 65), TestUidGenerator.Generate(), TestUidGenerator.Generate()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.DeleteInstanceAsync(TestUidGenerator.Generate(), null, TestUidGenerator.Generate()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteInstanceAsync(TestUidGenerator.Generate(), string.Empty, TestUidGenerator.Generate()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteInstanceAsync(TestUidGenerator.Generate(), new string('a', 65), TestUidGenerator.Generate()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.DeleteInstanceAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteInstanceAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteInstanceAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), new string('a', 65)));
        }

        private static DicomDataset CreateRandomInstanceDataset(
            string studyInstanceUID,
            string seriesInstanceUID,
            string sopInstanceUID)
        {
            var result = new DicomDataset();
            AddIfNotNull(result, DicomTag.StudyInstanceUID, studyInstanceUID);
            AddIfNotNull(result, DicomTag.SeriesInstanceUID, seriesInstanceUID);
            AddIfNotNull(result, DicomTag.SOPInstanceUID, sopInstanceUID);
            return result;
        }

        private static void AddIfNotNull(DicomDataset dicomDataset, DicomTag dicomTag, string value)
        {
            if (value != null)
            {
                dicomDataset.Add(dicomTag, value);
            }
        }
    }
}
