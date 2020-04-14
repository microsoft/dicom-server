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
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
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
        private readonly IDicomRetrieveMetadataService _dicomRetrieveMetadataService;

        public DicomRetrieveMetadataServiceTests()
        {
            _dicomInstanceStore = Substitute.For<IDicomInstanceStore>();
            _dicomMetadataStore = Substitute.For<IDicomMetadataStore>();
            _logger = Substitute.For<ILogger<DicomRetrieveMetadataService>>();

            _dicomRetrieveMetadataService = new DicomRetrieveMetadataService(_dicomInstanceStore, _dicomMetadataStore, _logger);
        }

        [Fact]
        public async Task GivenAStudyInstanceUid_WhenRetireveInstanceMetadataFailsToRetrieveAny_ThenDicomInstanceMetadataNotFoundExceptionIsThrownAsync()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid1 = TestUidGenerator.Generate();
            string sopInstanceUid2 = TestUidGenerator.Generate();

            DicomInstanceIdentifier id1 = new DicomInstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid1);
            DicomInstanceIdentifier id2 = new DicomInstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid2);

            List<DicomInstanceIdentifier> dicomInstanceIdentifiersList = new List<DicomInstanceIdentifier> { id1, id2 };
            _dicomInstanceStore.GetInstanceIdentifiersInStudyAsync(studyInstanceUid, CancellationToken.None).Returns(dicomInstanceIdentifiersList);

            _dicomMetadataStore.GetInstanceMetadataAsync(id1).Throws(new DicomDataStoreException());
            _dicomMetadataStore.GetInstanceMetadataAsync(id2).Throws(new DicomDataStoreException());

            DicomRetrieveMetadataRequest request = new DicomRetrieveMetadataRequest(studyInstanceUid);

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid, CancellationToken.None));
        }

        [Fact]
        public async Task GivenAStudyInstanceUid_WhenRetireveInstanceMetadataFailsToRetrieveAll_ThenPartialContentRetrievedStatusCodeIsReturnedAsync()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid1 = TestUidGenerator.Generate();
            string sopInstanceUid2 = TestUidGenerator.Generate();

            DicomInstanceIdentifier id1 = new DicomInstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid1);
            DicomInstanceIdentifier id2 = new DicomInstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid2);

            List<DicomInstanceIdentifier> dicomInstanceIdentifiersList = new List<DicomInstanceIdentifier> { id1, id2 };
            _dicomInstanceStore.GetInstanceIdentifiersInStudyAsync(studyInstanceUid, CancellationToken.None).Returns(dicomInstanceIdentifiersList);

            _dicomMetadataStore.GetInstanceMetadataAsync(id2, CancellationToken.None).Throws(new DicomDataStoreException());
            _dicomMetadataStore.GetInstanceMetadataAsync(id1, CancellationToken.None).Returns(new DicomDataset());

            DicomRetrieveMetadataRequest request = new DicomRetrieveMetadataRequest(studyInstanceUid);

            DicomRetrieveMetadataResponse response = await _dicomRetrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid, CancellationToken.None);

            Assert.Equal((int)HttpStatusCode.PartialContent, response.StatusCode);
            Assert.True(response.ResponseMetadata.Count() < dicomInstanceIdentifiersList.Count());
        }

        [Fact]
        public async Task GivenAStudyInstanceUid_WhenRetireveInstanceMetadataSucceedsToRetrieveAll_ThenSuccessStatusCodeIsReturnedAsync()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid1 = TestUidGenerator.Generate();
            string sopInstanceUid2 = TestUidGenerator.Generate();

            DicomInstanceIdentifier id1 = new DicomInstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid1);
            DicomInstanceIdentifier id2 = new DicomInstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid2);

            List<DicomInstanceIdentifier> dicomInstanceIdentifiersList = new List<DicomInstanceIdentifier> { id1, id2 };
            _dicomInstanceStore.GetInstanceIdentifiersInStudyAsync(studyInstanceUid, CancellationToken.None).Returns(dicomInstanceIdentifiersList);

            _dicomMetadataStore.GetInstanceMetadataAsync(id1, CancellationToken.None).Returns(new DicomDataset());
            _dicomMetadataStore.GetInstanceMetadataAsync(id2, CancellationToken.None).Returns(new DicomDataset());

            DicomRetrieveMetadataRequest request = new DicomRetrieveMetadataRequest(studyInstanceUid);

            DicomRetrieveMetadataResponse response = await _dicomRetrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid, CancellationToken.None);

            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(response.ResponseMetadata.Count(), dicomInstanceIdentifiersList.Count());
        }
    }
}
