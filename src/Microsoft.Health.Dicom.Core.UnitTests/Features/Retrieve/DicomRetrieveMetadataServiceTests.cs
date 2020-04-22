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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Exceptions;
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
        private readonly ILogger<DicomRetrieveMetadataService> _logger;
        private readonly DicomRetrieveMetadataService _dicomRetrieveMetadataService;

        private readonly string studyInstanceUid = TestUidGenerator.Generate();
        private readonly string seriesInstanceUid = TestUidGenerator.Generate();
        private readonly string sopInstanceUid = TestUidGenerator.Generate();
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        public DicomRetrieveMetadataServiceTests()
        {
            _dicomInstanceStore = Substitute.For<IDicomInstanceStore>();
            _dicomMetadataStore = Substitute.For<IDicomMetadataStore>();
            _logger = NullLogger<DicomRetrieveMetadataService>.Instance;

            _dicomRetrieveMetadataService = new DicomRetrieveMetadataService(_dicomInstanceStore, _dicomMetadataStore, _logger);
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForStudy_WhenFailsToRetrieveAny_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiersList = SetupInstanceIdentifiersList(ResourceType.Study);

            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.First(), DefaultCancellationToken).Throws(new DicomDataStoreException(HttpStatusCode.NotFound));

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForStudy_WhenFailsToRetrieveAll_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiersList = SetupInstanceIdentifiersList(ResourceType.Study);

            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.Last(), DefaultCancellationToken).Throws(new DicomDataStoreException(HttpStatusCode.NotFound));
            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.First(), DefaultCancellationToken).Returns(new DicomDataset());

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForStudy_WhenIsSuccessful_ThenSuccessStatusCodeIsReturnedAsync()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiersList = SetupInstanceIdentifiersList(ResourceType.Study);

            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.First(), DefaultCancellationToken).Returns(new DicomDataset());
            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.Last(), DefaultCancellationToken).Returns(new DicomDataset());

            DicomRetrieveMetadataResponse response = await _dicomRetrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid, DefaultCancellationToken);

            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(response.ResponseMetadata.Count(), instanceIdentifiersList.Count());
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForSeries_WhenIsSuccessful_ThenSuccessStatusCodeIsReturnedAsync()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiersList = SetupInstanceIdentifiersList(ResourceType.Series);

            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.First(), DefaultCancellationToken).Returns(new DicomDataset());
            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.Last(), DefaultCancellationToken).Returns(new DicomDataset());

            DicomRetrieveMetadataResponse response = await _dicomRetrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, DefaultCancellationToken);

            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(response.ResponseMetadata.Count(), instanceIdentifiersList.Count());
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForSeries_WhenFailsToRetrieveAll_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiersList = SetupInstanceIdentifiersList(ResourceType.Series);

            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.Last(), DefaultCancellationToken).Throws(new DicomDataStoreException(HttpStatusCode.NotFound));
            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.First(), DefaultCancellationToken).Returns(new DicomDataset());

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForSeries_WhenFailsToRetrieveAny_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiersList = SetupInstanceIdentifiersList(ResourceType.Series);

            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.Last(), DefaultCancellationToken).Throws(new DicomDataStoreException(HttpStatusCode.NotFound));
            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.First(), DefaultCancellationToken).Returns(new DicomDataset());

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForInstance_WhenIsSuccessful_ThenSuccessStatusCodeIsReturnedAsync()
        {
            VersionedDicomInstanceIdentifier sopInstanceIdentifier = SetupInstanceIdentifiersList(ResourceType.Instance).First();

            _dicomMetadataStore.GetInstanceMetadataAsync(sopInstanceIdentifier, DefaultCancellationToken).Returns(new DicomDataset());

            DicomRetrieveMetadataResponse response = await _dicomRetrieveMetadataService.RetrieveSopInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, DefaultCancellationToken);

            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
            Assert.Single(response.ResponseMetadata);
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForInstance_WhenFailsToRetrieve_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            VersionedDicomInstanceIdentifier sopInstanceIdentifier = SetupInstanceIdentifiersList(ResourceType.Instance).First();

            _dicomMetadataStore.GetInstanceMetadataAsync(sopInstanceIdentifier, DefaultCancellationToken).Throws(new DicomDataStoreException(HttpStatusCode.NotFound));

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveSopInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, DefaultCancellationToken));
        }

        private List<VersionedDicomInstanceIdentifier> SetupInstanceIdentifiersList(ResourceType resourceType)
        {
            var dicomInstanceIdentifiersList = new List<VersionedDicomInstanceIdentifier>();

            switch (resourceType)
            {
                case ResourceType.Study:
                    dicomInstanceIdentifiersList.Add(new VersionedDicomInstanceIdentifier(studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate(), version: 0));
                    dicomInstanceIdentifiersList.Add(new VersionedDicomInstanceIdentifier(studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate(), version: 1));
                    _dicomInstanceStore.GetInstanceIdentifiersInStudyAsync(studyInstanceUid, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                    break;
                case ResourceType.Series:
                    dicomInstanceIdentifiersList.Add(new VersionedDicomInstanceIdentifier(studyInstanceUid, seriesInstanceUid, TestUidGenerator.Generate(), version: 0));
                    dicomInstanceIdentifiersList.Add(new VersionedDicomInstanceIdentifier(studyInstanceUid, seriesInstanceUid, TestUidGenerator.Generate(), version: 1));
                    _dicomInstanceStore.GetInstanceIdentifiersInSeriesAsync(studyInstanceUid, seriesInstanceUid, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                    break;
                case ResourceType.Instance:
                    dicomInstanceIdentifiersList.Add(new VersionedDicomInstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid, version: 0));
                    _dicomInstanceStore.GetInstanceIdentifierAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                    break;
            }

            return dicomInstanceIdentifiersList;
        }
    }
}
