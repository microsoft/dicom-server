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
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
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
    public class RetrieveMetadataServiceTests : IClassFixture<DataStoreTestsFixture>
    {
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private readonly RetrieveMetadataService _retrieveMetadataService;
        private readonly IInstanceStore _instanceStore;
        private readonly IMetadataStore _metadataStore;

        private readonly string _studyInstanceUid = TestUidGenerator.Generate();
        private readonly string _seriesInstanceUid = TestUidGenerator.Generate();

        public RetrieveMetadataServiceTests(DataStoreTestsFixture storagefixture)
        {
            _instanceStore = Substitute.For<IInstanceStore>();
            _metadataStore = storagefixture.MetadataStore;
            _retrieveMetadataService = new RetrieveMetadataService(_instanceStore, _metadataStore);
        }

        [Fact]
        public async Task GivenRetrieveMetadataRequestForStudy_WhenFailsToRetrieveAll_ThenNotFoundIsThrown()
        {
            List<DicomDataset> datasetList = SetupDatasetList(ResourceType.Study);

            // Add metadata for only one instance in the given list
            await _metadataStore.StoreInstanceMetadataAsync(datasetList.Last(), version: 0);

            await Assert.ThrowsAsync<ItemNotFoundException>(() => _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(_studyInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveMetadataRequestForStudy_WhenFailsToRetrieveAny_ThenNotFoundIsThrown()
        {
            SetupDatasetList(ResourceType.Study);

            await Assert.ThrowsAsync<ItemNotFoundException>(() => _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(_studyInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveMetadataRequestForStudy_WhenIsSuccessful_ThenInstanceMetadataIsRetrievedSuccessfully()
        {
            List<DicomDataset> datasetList = SetupDatasetList(ResourceType.Study);

            // Add metadata for all instances in the given list
            await _metadataStore.StoreInstanceMetadataAsync(datasetList.First(), version: 0);
            await _metadataStore.StoreInstanceMetadataAsync(datasetList.Last(), version: 1);

            RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(_studyInstanceUid, DefaultCancellationToken);

            ValidateResponseMetadataDataset(datasetList.First(), response.ResponseMetadata.First());
            ValidateResponseMetadataDataset(datasetList.Last(), response.ResponseMetadata.Last());
        }

        [Fact]
        public async Task GivenRetrieveMetadataRequestForSeries_WhenFailsToRetrieveAll_ThenNotFoundIsThrown()
        {
            List<DicomDataset> datasetList = SetupDatasetList(ResourceType.Series);

            // Add metadata for only one instance in the given list
            await _metadataStore.StoreInstanceMetadataAsync(datasetList.Last(), version: 1);

            await Assert.ThrowsAsync<ItemNotFoundException>(() => _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveMetadataRequestForSeries_WhenFailsToRetrieveAny_ThenNotFoundIsThrown()
        {
            SetupDatasetList(ResourceType.Series);

            await Assert.ThrowsAsync<ItemNotFoundException>(() => _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveMetadataRequestForSeries_WhenIsSuccessful_ThenInstanceMetadataIsRetrievedSuccessfully()
        {
            List<DicomDataset> datasetList = SetupDatasetList(ResourceType.Series);

            // Add metadata for all instances in the given list
            await _metadataStore.StoreInstanceMetadataAsync(datasetList.First(), version: 0);
            await _metadataStore.StoreInstanceMetadataAsync(datasetList.Last(), version: 1);

            RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, DefaultCancellationToken);

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

                    _instanceStore.GetInstanceIdentifiersInStudyAsync(_studyInstanceUid, DefaultCancellationToken).Returns(new List<VersionedInstanceIdentifier>()
                    {
                        dicomDataset1.ToVersionedInstanceIdentifier(version: 0),
                        dicomDataset2.ToVersionedInstanceIdentifier(version: 1),
                    });
                    break;

                case ResourceType.Series:
                    dicomDataset1 = CreateValidMetadataDataset(_studyInstanceUid, _seriesInstanceUid, TestUidGenerator.Generate());
                    dicomDataset2 = CreateValidMetadataDataset(_studyInstanceUid, _seriesInstanceUid, TestUidGenerator.Generate());

                    _instanceStore.GetInstanceIdentifiersInSeriesAsync(_studyInstanceUid, _seriesInstanceUid, DefaultCancellationToken).Returns(new List<VersionedInstanceIdentifier>()
                    {
                        dicomDataset1.ToVersionedInstanceIdentifier(version: 0),
                        dicomDataset2.ToVersionedInstanceIdentifier(version: 1),
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
