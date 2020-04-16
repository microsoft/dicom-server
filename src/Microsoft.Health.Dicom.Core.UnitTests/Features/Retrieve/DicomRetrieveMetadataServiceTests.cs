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
            List<DicomInstanceIdentifier> instanceIdentifiersList = SetupInstanceIdentifiersList(ResourceType.Study);

            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.First()).Throws(new DicomDataStoreException(HttpStatusCode.NotFound));

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid, CancellationToken.None));
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForStudy_WhenFailsToRetrieveAll_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            List<DicomInstanceIdentifier> instanceIdentifiersList = SetupInstanceIdentifiersList(ResourceType.Study);

            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.Last(), CancellationToken.None).Throws(new DicomDataStoreException(HttpStatusCode.NotFound));
            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.First(), CancellationToken.None).Returns(new DicomDataset());

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid, CancellationToken.None));
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForStudy_WhenIsSuccessful_ThenSuccessStatusCodeIsReturnedAsync()
        {
            List<DicomInstanceIdentifier> instanceIdentifiersList = SetupInstanceIdentifiersList(ResourceType.Study);

            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.First(), CancellationToken.None).Returns(new DicomDataset());
            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.Last(), CancellationToken.None).Returns(new DicomDataset());

            DicomRetrieveMetadataResponse response = await _dicomRetrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid, CancellationToken.None);

            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(response.ResponseMetadata.Count(), instanceIdentifiersList.Count());
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForSeries_WhenIsSuccessful_ThenSuccessStatusCodeIsReturnedAsync()
        {
            List<DicomInstanceIdentifier> instanceIdentifiersList = SetupInstanceIdentifiersList(ResourceType.Series);

            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.First(), CancellationToken.None).Returns(new DicomDataset());
            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.Last(), CancellationToken.None).Returns(new DicomDataset());

            DicomRetrieveMetadataResponse response = await _dicomRetrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, CancellationToken.None);

            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(response.ResponseMetadata.Count(), instanceIdentifiersList.Count());
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForSeries_WhenFailsToRetrieveAll_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            List<DicomInstanceIdentifier> instanceIdentifiersList = SetupInstanceIdentifiersList(ResourceType.Series);

            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.Last(), CancellationToken.None).Throws(new DicomDataStoreException(HttpStatusCode.NotFound));
            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.First(), CancellationToken.None).Returns(new DicomDataset());

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, CancellationToken.None));
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForSeries_WhenFailsToRetrieveAny_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            List<DicomInstanceIdentifier> instanceIdentifiersList = SetupInstanceIdentifiersList(ResourceType.Series);

            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.Last()).Throws(new DicomDataStoreException(HttpStatusCode.NotFound));
            _dicomMetadataStore.GetInstanceMetadataAsync(instanceIdentifiersList.First(), CancellationToken.None).Returns(new DicomDataset());

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, CancellationToken.None));
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForInstance_WhenIsSuccessful_ThenSuccessStatusCodeIsReturnedAsync()
        {
            DicomInstanceIdentifier sopInstanceIdentifier = SetupInstanceIdentifiersList(ResourceType.Instance).First();

            _dicomMetadataStore.GetInstanceMetadataAsync(sopInstanceIdentifier, CancellationToken.None).Returns(new DicomDataset());

            DicomRetrieveMetadataResponse response = await _dicomRetrieveMetadataService.RetrieveSopInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, CancellationToken.None);

            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
            Assert.Single(response.ResponseMetadata);
        }

        [Fact]
        public async Task GivenRetrieveInstanceMetadataRequestForInstance_WhenFailsToRetrieve_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
        {
            DicomInstanceIdentifier sopInstanceIdentifier = SetupInstanceIdentifiersList(ResourceType.Instance).First();

            _dicomMetadataStore.GetInstanceMetadataAsync(sopInstanceIdentifier, CancellationToken.None).Throws(new DicomDataStoreException(HttpStatusCode.NotFound));

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveSopInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, CancellationToken.None));
        }

        private List<DicomInstanceIdentifier> SetupInstanceIdentifiersList(ResourceType resourceType)
        {
            var dicomInstanceIdentifiersList = new List<DicomInstanceIdentifier>();

            switch (resourceType)
            {
                case ResourceType.Study:
                    dicomInstanceIdentifiersList.Add(new DicomInstanceIdentifier(studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate()));
                    dicomInstanceIdentifiersList.Add(new DicomInstanceIdentifier(studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate()));
                    _dicomInstanceStore.GetInstanceIdentifiersInStudyAsync(studyInstanceUid, CancellationToken.None).Returns(dicomInstanceIdentifiersList);
                    break;
                case ResourceType.Series:
                    dicomInstanceIdentifiersList.Add(new DicomInstanceIdentifier(studyInstanceUid, seriesInstanceUid, TestUidGenerator.Generate()));
                    dicomInstanceIdentifiersList.Add(new DicomInstanceIdentifier(studyInstanceUid, seriesInstanceUid, TestUidGenerator.Generate()));
                    _dicomInstanceStore.GetInstanceIdentifiersInSeriesAsync(studyInstanceUid, seriesInstanceUid, CancellationToken.None).Returns(dicomInstanceIdentifiersList);
                    break;
                case ResourceType.Instance:
                    dicomInstanceIdentifiersList.Add(new DicomInstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
                    _dicomInstanceStore.GetInstanceIdentifierAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, CancellationToken.None).Returns(dicomInstanceIdentifiersList);
                    break;
            }

            return dicomInstanceIdentifiersList;
        }
    }
}
