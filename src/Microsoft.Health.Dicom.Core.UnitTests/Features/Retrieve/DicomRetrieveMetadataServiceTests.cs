// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using DicomInstanceNotFoundException = Microsoft.Health.Dicom.Core.Exceptions.DicomInstanceNotFoundException;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve
{
    public class DicomRetrieveMetadataServiceTests
    {
        private readonly IDicomInstanceStore _dicomInstanceStore;
        private readonly IDicomMetadataStore _dicomMetadataStore;
        private readonly DicomRetrieveMetadataService _dicomRetrieveMetadataService;

        private readonly string _studyInstanceUid = TestUidGenerator.Generate();
        private readonly string _seriesInstanceUid = TestUidGenerator.Generate();
        private readonly string _sopInstanceUid = TestUidGenerator.Generate();
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        public DicomRetrieveMetadataServiceTests()
        {
            _dicomInstanceStore = Substitute.For<IDicomInstanceStore>();
            _dicomMetadataStore = Substitute.For<IDicomMetadataStore>();

            _dicomRetrieveMetadataService = new DicomRetrieveMetadataService(_dicomInstanceStore, _dicomMetadataStore);
        }

        [Fact]
        public async Task GivenRetrieveStudyMetadataRequest_WhenStudyInstanceUidDoesnotExists_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            var exception = await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveStudyInstanceMetadataAsync(TestUidGenerator.Generate(), DefaultCancellationToken));
            Assert.Equal("The specified study instance cannot be found.", exception.Message);
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenStudyandSeriesInstanceUidDoesnotExists_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            var exception = await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), DefaultCancellationToken));
            Assert.Equal("The specified series instance cannot be found.", exception.Message);
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenStudyInstanceUidDoesnotExists_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            SetupInstanceIdentifiersList(ResourceType.Series);

            var exception = await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(TestUidGenerator.Generate(), _seriesInstanceUid, DefaultCancellationToken));
            Assert.Equal("The specified series instance cannot be found.", exception.Message);
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenSeriesInstanceUidDoesnotExists_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            SetupInstanceIdentifiersList(ResourceType.Series);

            var exception = await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(_studyInstanceUid, TestUidGenerator.Generate(), DefaultCancellationToken));
            Assert.Equal("The specified series instance cannot be found.", exception.Message);
        }

        [Fact]
        public async Task GivenRetrieveSopInstanceMetadataRequest_WhenStudySeriesAndSopInstanceUidDoesnotExists_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            var exception = await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveSopInstanceMetadataAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), DefaultCancellationToken));
            Assert.Equal("The specified instance cannot be found.", exception.Message);
        }

        [Fact]
        public async Task GivenRetrieveSopInstanceMetadataRequest_WhenStudyAndSeriesDoesnotExists_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            SetupInstanceIdentifiersList(ResourceType.Instance);

            var exception = await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveSopInstanceMetadataAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), _sopInstanceUid, DefaultCancellationToken));
            Assert.Equal("The specified instance cannot be found.", exception.Message);
        }

        [Fact]
        public async Task GivenRetrieveSopInstanceMetadataRequest_WhenSeriesInstanceUidDoesnotExists_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            SetupInstanceIdentifiersList(ResourceType.Instance);

            var exception = await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveSopInstanceMetadataAsync(_studyInstanceUid, TestUidGenerator.Generate(), _sopInstanceUid, DefaultCancellationToken));
            Assert.Equal("The specified instance cannot be found.", exception.Message);
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForStudy_WhenFailsToRetrieveSome_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiersList = SetupInstanceIdentifiersList(ResourceType.Study);

            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.Last(), DefaultCancellationToken).Throws(new DicomInstanceNotFoundException());
            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.First(), DefaultCancellationToken).Returns(new DicomDataset());

            var exception = await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveStudyInstanceMetadataAsync(_studyInstanceUid, DefaultCancellationToken));
            Assert.Equal("The specified instance cannot be found.", exception.Message);
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForStudy_WhenIsSuccessful_ThenSuccessStatusCodeIsReturnedAsync()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiersList = SetupInstanceIdentifiersList(ResourceType.Study);

            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.First(), DefaultCancellationToken).Returns(new DicomDataset());
            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.Last(), DefaultCancellationToken).Returns(new DicomDataset());

            DicomRetrieveMetadataResponse response = await _dicomRetrieveMetadataService.RetrieveStudyInstanceMetadataAsync(_studyInstanceUid, DefaultCancellationToken);

            Assert.Equal(response.ResponseMetadata.Count(), instanceIdentifiersList.Count());
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForSeries_WhenFailsToRetrieveSome_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiersList = SetupInstanceIdentifiersList(ResourceType.Series);

            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.Last(), DefaultCancellationToken).Throws(new DicomInstanceNotFoundException());
            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.First(), DefaultCancellationToken).Returns(new DicomDataset());

            var exception = await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, DefaultCancellationToken));
            Assert.Equal("The specified instance cannot be found.", exception.Message);
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForSeries_WhenIsSuccessful_ThenSuccessStatusCodeIsReturnedAsync()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiersList = SetupInstanceIdentifiersList(ResourceType.Series);

            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.First(), DefaultCancellationToken).Returns(new DicomDataset());
            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.Last(), DefaultCancellationToken).Returns(new DicomDataset());

            DicomRetrieveMetadataResponse response = await _dicomRetrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, DefaultCancellationToken);

            Assert.Equal(response.ResponseMetadata.Count(), instanceIdentifiersList.Count());
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForInstance_WhenFailsToRetrieve_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            VersionedDicomInstanceIdentifier sopInstanceIdentifier = SetupInstanceIdentifiersList(ResourceType.Instance).First();

            _dicomMetadataStore.GetInstanceMetadataAsync(sopInstanceIdentifier, DefaultCancellationToken).Throws(new DicomInstanceNotFoundException());

            var exception = await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveSopInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, _sopInstanceUid, DefaultCancellationToken));
            Assert.Equal("The specified instance cannot be found.", exception.Message);
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForInstance_WhenIsSuccessful_ThenSuccessStatusCodeIsReturnedAsync()
        {
            VersionedDicomInstanceIdentifier sopInstanceIdentifier = SetupInstanceIdentifiersList(ResourceType.Instance).First();

            _dicomMetadataStore.GetInstanceMetadataAsync(sopInstanceIdentifier, DefaultCancellationToken).Returns(new DicomDataset());

            DicomRetrieveMetadataResponse response = await _dicomRetrieveMetadataService.RetrieveSopInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, _sopInstanceUid, DefaultCancellationToken);

            Assert.Single(response.ResponseMetadata);
        }

        private List<VersionedDicomInstanceIdentifier> SetupInstanceIdentifiersList(ResourceType resourceType)
        {
            var dicomInstanceIdentifiersList = new List<VersionedDicomInstanceIdentifier>();

            switch (resourceType)
            {
                case ResourceType.Study:
                    dicomInstanceIdentifiersList.Add(new VersionedDicomInstanceIdentifier(_studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate(), version: 0));
                    dicomInstanceIdentifiersList.Add(new VersionedDicomInstanceIdentifier(_studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate(), version: 1));
                    _dicomInstanceStore.GetInstanceIdentifiersInStudyAsync(_studyInstanceUid, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                    break;
                case ResourceType.Series:
                    dicomInstanceIdentifiersList.Add(new VersionedDicomInstanceIdentifier(_studyInstanceUid, _seriesInstanceUid, TestUidGenerator.Generate(), version: 0));
                    dicomInstanceIdentifiersList.Add(new VersionedDicomInstanceIdentifier(_studyInstanceUid, _seriesInstanceUid, TestUidGenerator.Generate(), version: 1));
                    _dicomInstanceStore.GetInstanceIdentifiersInSeriesAsync(_studyInstanceUid, _seriesInstanceUid, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                    break;
                case ResourceType.Instance:
                    dicomInstanceIdentifiersList.Add(new VersionedDicomInstanceIdentifier(_studyInstanceUid, _seriesInstanceUid, _sopInstanceUid, version: 0));
                    _dicomInstanceStore.GetInstanceIdentifierAsync(_studyInstanceUid, _seriesInstanceUid, _sopInstanceUid, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                    break;
            }

            return dicomInstanceIdentifiersList;
        }
    }
}
