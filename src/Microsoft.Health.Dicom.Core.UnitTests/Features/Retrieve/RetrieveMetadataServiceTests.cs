// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve
{
    public class RetrieveMetadataServiceTests
    {
        private readonly IInstanceStore _instanceStore;
        private readonly IMetadataStore _metadataStore;
        private readonly RetrieveMetadataService _retrieveMetadataService;

        private readonly string _studyInstanceUid = TestUidGenerator.Generate();
        private readonly string _seriesInstanceUid = TestUidGenerator.Generate();
        private readonly string _sopInstanceUid = TestUidGenerator.Generate();
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        public RetrieveMetadataServiceTests()
        {
            _instanceStore = Substitute.For<IInstanceStore>();
            _metadataStore = Substitute.For<IMetadataStore>();

            _retrieveMetadataService = new RetrieveMetadataService(_instanceStore, _metadataStore);
        }

        [Fact]
        public async Task GivenRetrieveStudyMetadataRequest_WhenStudyInstanceUidDoesNotExist_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(TestUidGenerator.Generate(), DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenStudyAndSeriesInstanceUidDoesNotExist_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenStudyInstanceUidDoesNotExist_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            SetupInstanceIdentifiersList(ResourceType.Series);

            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(TestUidGenerator.Generate(), _seriesInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveSeriesMetadataRequest_WhenSeriesInstanceUidDoesNotExist_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            SetupInstanceIdentifiersList(ResourceType.Series);

            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(_studyInstanceUid, TestUidGenerator.Generate(), DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveSopInstanceMetadataRequest_WhenStudySeriesAndSopInstanceUidDoesNotExist_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveMetadataService.RetrieveSopInstanceMetadataAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveSopInstanceMetadataRequest_WhenStudyAndSeriesDoesNotExist_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            SetupInstanceIdentifiersList(ResourceType.Instance);

            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveMetadataService.RetrieveSopInstanceMetadataAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), _sopInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveSopInstanceMetadataRequest_WhenSeriesInstanceUidDoesNotExist_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            SetupInstanceIdentifiersList(ResourceType.Instance);

            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveMetadataService.RetrieveSopInstanceMetadataAsync(_studyInstanceUid, TestUidGenerator.Generate(), _sopInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForStudy_WhenFailsToRetrieveSome_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            List<VersionedInstanceIdentifier> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Study);

            _metadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifiers.Last(), DefaultCancellationToken).Throws(new InstanceNotFoundException());
            _metadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifiers.First(), DefaultCancellationToken).Returns(new DicomDataset());

            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(_studyInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForStudy_WhenIsSuccessful_ThenSuccessStatusCodeIsReturnedAsync()
        {
            List<VersionedInstanceIdentifier> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Study);

            _metadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifiers.First(), DefaultCancellationToken).Returns(new DicomDataset());
            _metadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifiers.Last(), DefaultCancellationToken).Returns(new DicomDataset());

            RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(_studyInstanceUid, DefaultCancellationToken);

            Assert.Equal(response.ResponseMetadata.Count(), versionedInstanceIdentifiers.Count());
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForSeries_WhenFailsToRetrieveSome_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            List<VersionedInstanceIdentifier> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Series);

            _metadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifiers.Last(), DefaultCancellationToken).Throws(new InstanceNotFoundException());
            _metadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifiers.First(), DefaultCancellationToken).Returns(new DicomDataset());

            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForSeries_WhenIsSuccessful_ThenSuccessStatusCodeIsReturnedAsync()
        {
            List<VersionedInstanceIdentifier> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Series);

            _metadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifiers.First(), DefaultCancellationToken).Returns(new DicomDataset());
            _metadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifiers.Last(), DefaultCancellationToken).Returns(new DicomDataset());

            RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, DefaultCancellationToken);

            Assert.Equal(response.ResponseMetadata.Count(), versionedInstanceIdentifiers.Count());
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForInstance_WhenFailsToRetrieve_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            VersionedInstanceIdentifier sopInstanceIdentifier = SetupInstanceIdentifiersList(ResourceType.Instance).First();

            _metadataStore.GetInstanceMetadataAsync(sopInstanceIdentifier, DefaultCancellationToken).Throws(new InstanceNotFoundException());

            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveMetadataService.RetrieveSopInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, _sopInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForInstance_WhenIsSuccessful_ThenSuccessStatusCodeIsReturnedAsync()
        {
            VersionedInstanceIdentifier sopInstanceIdentifier = SetupInstanceIdentifiersList(ResourceType.Instance).First();

            _metadataStore.GetInstanceMetadataAsync(sopInstanceIdentifier, DefaultCancellationToken).Returns(new DicomDataset());

            RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveSopInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, _sopInstanceUid, DefaultCancellationToken);

            Assert.Single(response.ResponseMetadata);
        }

        private List<VersionedInstanceIdentifier> SetupInstanceIdentifiersList(ResourceType resourceType)
        {
            var dicomInstanceIdentifiersList = new List<VersionedInstanceIdentifier>();

            switch (resourceType)
            {
                case ResourceType.Study:
                    dicomInstanceIdentifiersList.Add(new VersionedInstanceIdentifier(_studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate(), version: 0));
                    dicomInstanceIdentifiersList.Add(new VersionedInstanceIdentifier(_studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate(), version: 1));
                    _instanceStore.GetInstanceIdentifiersInStudyAsync(_studyInstanceUid, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                    break;
                case ResourceType.Series:
                    dicomInstanceIdentifiersList.Add(new VersionedInstanceIdentifier(_studyInstanceUid, _seriesInstanceUid, TestUidGenerator.Generate(), version: 0));
                    dicomInstanceIdentifiersList.Add(new VersionedInstanceIdentifier(_studyInstanceUid, _seriesInstanceUid, TestUidGenerator.Generate(), version: 1));
                    _instanceStore.GetInstanceIdentifiersInSeriesAsync(_studyInstanceUid, _seriesInstanceUid, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                    break;
                case ResourceType.Instance:
                    dicomInstanceIdentifiersList.Add(new VersionedInstanceIdentifier(_studyInstanceUid, _seriesInstanceUid, _sopInstanceUid, version: 0));
                    _instanceStore.GetInstanceIdentifierAsync(_studyInstanceUid, _seriesInstanceUid, _sopInstanceUid, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                    break;
            }

            return dicomInstanceIdentifiersList;
        }
    }
}
