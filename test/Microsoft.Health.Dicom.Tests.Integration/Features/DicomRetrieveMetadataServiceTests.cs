// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Dicom.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Integration.Persistence;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Features
{
    public class DicomRetrieveMetadataServiceTests : IClassFixture<DicomBlobStorageTestsFixture>
    {
        private readonly DicomRetrieveMetadataService _dicomRetrieveMetadataService;
        private readonly IDicomInstanceStore _dicomInstanceStore;
        private readonly IDicomMetadataStore _dicomMetadataStore;
        private readonly ILogger<DicomRetrieveMetadataService> _logger;
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private readonly string studyInstanceUid = TestUidGenerator.Generate();
        private readonly string seriesInstanceUid = TestUidGenerator.Generate();

        public DicomRetrieveMetadataServiceTests(DicomBlobStorageTestsFixture storagefixture)
        {
            _dicomInstanceStore = Substitute.For<IDicomInstanceStore>();
            _dicomMetadataStore = storagefixture.DicomMetadataStore;
            _logger = NullLogger<DicomRetrieveMetadataService>.Instance;
            _dicomRetrieveMetadataService = new DicomRetrieveMetadataService(_dicomInstanceStore, _dicomMetadataStore, _logger);
        }

        [Fact]
        public async Task GivenRetrieveMetadataRequestForStudy_WhenFailsToRetrieveAll_ThenNotFoundIsThrown()
        {
            List<DicomDataset> datasetList = SetupDatasetList(ResourceType.Study);

            // Add metadata for only one instance in the given list
            await _dicomMetadataStore.AddInstanceMetadataAsync(datasetList.Last());

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveMetadataRequestForStudy_WhenFailsToRetrieveAny_ThenNotFoundIsThrown()
        {
            SetupDatasetList(ResourceType.Study);

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveMetadataRequestForStudy_WhenIsSuccessful_ThenInstanceMetadataIsRetrievedSuccessfully()
        {
            List<DicomDataset> datasetList = SetupDatasetList(ResourceType.Study);

            // Add metadata for all instances in the given list
            await _dicomMetadataStore.AddInstanceMetadataAsync(datasetList.First());
            await _dicomMetadataStore.AddInstanceMetadataAsync(datasetList.Last());

            DicomRetrieveMetadataResponse response = await _dicomRetrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid, DefaultCancellationToken);
            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);

            ValidateResponseMetadataDataset(datasetList.First(), response.ResponseMetadata.First());
            ValidateResponseMetadataDataset(datasetList.Last(), response.ResponseMetadata.Last());
        }

        [Fact]
        public async Task GivenRetrieveMetadataRequestForSeries_WhenFailsToRetrieveAll_ThenNotFoundIsThrown()
        {
            List<DicomDataset> datasetList = SetupDatasetList(ResourceType.Series);

            // Add metadata for only one instance in the given list
            await _dicomMetadataStore.AddInstanceMetadataAsync(datasetList.Last());

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveMetadataRequestForSeries_WhenFailsToRetrieveAny_ThenNotFoundIsThrown()
        {
            SetupDatasetList(ResourceType.Series);

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveMetadataRequestForSeries_WhenIsSuccessful_ThenInstanceMetadataIsRetrievedSuccessfully()
        {
            List<DicomDataset> datasetList = SetupDatasetList(ResourceType.Series);

            // Add metadata for all instances in the given list
            await _dicomMetadataStore.AddInstanceMetadataAsync(datasetList.First());
            await _dicomMetadataStore.AddInstanceMetadataAsync(datasetList.Last());

            DicomRetrieveMetadataResponse response = await _dicomRetrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, DefaultCancellationToken);
            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);

            ValidateResponseMetadataDataset(datasetList.First(), response.ResponseMetadata.First());
            ValidateResponseMetadataDataset(datasetList.Last(), response.ResponseMetadata.Last());
        }

        private List<DicomDataset> SetupDatasetList(ResourceType resourceType)
        {
            DicomDataset dicomDataset1 = new DicomDataset();
            DicomDataset dicomDataset2 = new DicomDataset();

            switch (resourceType)
            {
                case ResourceType.Study:
                    dicomDataset1 = CreateValidMetadataDataset(studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate());
                    dicomDataset2 = CreateValidMetadataDataset(studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate());

                    _dicomInstanceStore.GetInstanceIdentifiersInStudyAsync(studyInstanceUid, DefaultCancellationToken).Returns(new List<DicomInstanceIdentifier>()
                    {
                        dicomDataset1.ToVersionedDicomInstanceIdentifier(version: 0),
                        dicomDataset2.ToVersionedDicomInstanceIdentifier(version: 0),
                    });
                    break;

                case ResourceType.Series:
                    dicomDataset1 = CreateValidMetadataDataset(studyInstanceUid, seriesInstanceUid, TestUidGenerator.Generate());
                    dicomDataset2 = CreateValidMetadataDataset(studyInstanceUid, seriesInstanceUid, TestUidGenerator.Generate());

                    _dicomInstanceStore.GetInstanceIdentifiersInSeriesAsync(studyInstanceUid, seriesInstanceUid, DefaultCancellationToken).Returns(new List<DicomInstanceIdentifier>()
                    {
                        dicomDataset1.ToVersionedDicomInstanceIdentifier(version: 0),
                        dicomDataset2.ToVersionedDicomInstanceIdentifier(version: 0),
                    });
                    break;
            }

            return new List<DicomDataset> { dicomDataset1, dicomDataset2 };
        }

        private DicomDataset CreateValidMetadataDataset(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            return new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, studyInstanceUid },
                { DicomTag.SeriesInstanceUID, seriesInstanceUid },
                { DicomTag.SOPInstanceUID, sopInstanceUid },
            };
        }

        private static void ValidateResponseMetadataDataset(DicomDataset storedDataset, DicomDataset retrievedDataset)
        {
            // Compare result datasets by serializing.
            var jsonDicomConverter = new JsonDicomConverter();
            Assert.Equal(
                JsonConvert.SerializeObject(storedDataset, jsonDicomConverter),
                JsonConvert.SerializeObject(retrievedDataset, jsonDicomConverter));
        }
    }
}
