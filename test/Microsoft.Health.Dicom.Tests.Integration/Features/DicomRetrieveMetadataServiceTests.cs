// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Dicom.Serialization;
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
    public class DicomRetrieveMetadataServiceTests : IClassFixture<DicomDataStoreTestsFixture>
    {
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private readonly DicomRetrieveMetadataService _dicomRetrieveMetadataService;
        private readonly IDicomInstanceStore _dicomInstanceStore;
        private readonly IDicomMetadataStore _dicomMetadataStore;

        private readonly string _studyInstanceUid = TestUidGenerator.Generate();
        private readonly string _seriesInstanceUid = TestUidGenerator.Generate();

        public DicomRetrieveMetadataServiceTests(DicomDataStoreTestsFixture storagefixture)
        {
            _dicomInstanceStore = Substitute.For<IDicomInstanceStore>();
            _dicomMetadataStore = storagefixture.DicomMetadataStore;
            _dicomRetrieveMetadataService = new DicomRetrieveMetadataService(_dicomInstanceStore, _dicomMetadataStore);
        }

        [Fact]
        public async Task GivenRetrieveMetadataRequestForStudy_WhenFailsToRetrieveAll_ThenNotFoundIsThrown()
        {
            List<DicomDataset> datasetList = SetupDatasetList(ResourceType.Study);

            // Add metadata for only one instance in the given list
            await _dicomMetadataStore.AddInstanceMetadataAsync(datasetList.Last(), version: 0);

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveStudyInstanceMetadataAsync(_studyInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveMetadataRequestForStudy_WhenFailsToRetrieveAny_ThenNotFoundIsThrown()
        {
            SetupDatasetList(ResourceType.Study);

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveStudyInstanceMetadataAsync(_studyInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveMetadataRequestForStudy_WhenIsSuccessful_ThenInstanceMetadataIsRetrievedSuccessfully()
        {
            List<DicomDataset> datasetList = SetupDatasetList(ResourceType.Study);

            // Add metadata for all instances in the given list
            await _dicomMetadataStore.AddInstanceMetadataAsync(datasetList.First(), version: 0);
            await _dicomMetadataStore.AddInstanceMetadataAsync(datasetList.Last(), version: 1);

            DicomRetrieveMetadataResponse response = await _dicomRetrieveMetadataService.RetrieveStudyInstanceMetadataAsync(_studyInstanceUid, DefaultCancellationToken);

            ValidateResponseMetadataDataset(datasetList.First(), response.ResponseMetadata.First());
            ValidateResponseMetadataDataset(datasetList.Last(), response.ResponseMetadata.Last());
        }

        [Fact]
        public async Task GivenRetrieveMetadataRequestForSeries_WhenFailsToRetrieveAll_ThenNotFoundIsThrown()
        {
            List<DicomDataset> datasetList = SetupDatasetList(ResourceType.Series);

            // Add metadata for only one instance in the given list
            await _dicomMetadataStore.AddInstanceMetadataAsync(datasetList.Last(), version: 1);

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveMetadataRequestForSeries_WhenFailsToRetrieveAny_ThenNotFoundIsThrown()
        {
            SetupDatasetList(ResourceType.Series);

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveMetadataRequestForSeries_WhenIsSuccessful_ThenInstanceMetadataIsRetrievedSuccessfully()
        {
            List<DicomDataset> datasetList = SetupDatasetList(ResourceType.Series);

            // Add metadata for all instances in the given list
            await _dicomMetadataStore.AddInstanceMetadataAsync(datasetList.First(), version: 0);
            await _dicomMetadataStore.AddInstanceMetadataAsync(datasetList.Last(), version: 1);

            DicomRetrieveMetadataResponse response = await _dicomRetrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, DefaultCancellationToken);

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
                    dicomDataset1 = CreateValidMetadataDataset(_studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate());
                    dicomDataset2 = CreateValidMetadataDataset(_studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate());

                    _dicomInstanceStore.GetInstanceIdentifiersInStudyAsync(_studyInstanceUid, DefaultCancellationToken).Returns(new List<VersionedDicomInstanceIdentifier>()
                    {
                        dicomDataset1.ToVersionedDicomInstanceIdentifier(version: 0),
                        dicomDataset2.ToVersionedDicomInstanceIdentifier(version: 1),
                    });
                    break;

                case ResourceType.Series:
                    dicomDataset1 = CreateValidMetadataDataset(_studyInstanceUid, _seriesInstanceUid, TestUidGenerator.Generate());
                    dicomDataset2 = CreateValidMetadataDataset(_studyInstanceUid, _seriesInstanceUid, TestUidGenerator.Generate());

                    _dicomInstanceStore.GetInstanceIdentifiersInSeriesAsync(_studyInstanceUid, _seriesInstanceUid, DefaultCancellationToken).Returns(new List<VersionedDicomInstanceIdentifier>()
                    {
                        dicomDataset1.ToVersionedDicomInstanceIdentifier(version: 0),
                        dicomDataset2.ToVersionedDicomInstanceIdentifier(version: 1),
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
