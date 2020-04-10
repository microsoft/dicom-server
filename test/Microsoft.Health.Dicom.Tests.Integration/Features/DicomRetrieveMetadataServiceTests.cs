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
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
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
        private readonly IDicomRetrieveMetadataService _dicomRetrieveMetadataService;
        private readonly IDicomInstanceStore _dicomInstanceStore;
        private readonly IDicomMetadataStore _dicomMetadataStore;
        private readonly ILogger<DicomRetrieveMetadataService> _logger;

        public DicomRetrieveMetadataServiceTests(DicomBlobStorageTestsFixture storagefixture)
        {
            _dicomInstanceStore = Substitute.For<IDicomInstanceStore>();
            _dicomMetadataStore = storagefixture.DicomMetadataStore;
            _logger = Substitute.For<ILogger<DicomRetrieveMetadataService>>();
            _dicomRetrieveMetadataService = new DicomRetrieveMetadataService(_dicomInstanceStore, _dicomMetadataStore, _logger);
        }

        [Fact]
        public async Task GivenAStudyInstanceUid_WhenRetireveInstanceMetadataFailsToRetrieveAll_ThenPartialContentRetrievedStatusCodeIsReturnedAsync()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid1 = TestUidGenerator.Generate();
            string sopInstanceUid2 = TestUidGenerator.Generate();

            DicomDataset dicomDataset = CreateValidMetadataDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid1);
            var dicomInstanceId = dicomDataset.ToVersionedDicomInstanceIdentifier(version: 0);

            DicomDataset dicomDataset1 = CreateValidMetadataDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid2);
            var dicomInstanceId2 = dicomDataset1.ToVersionedDicomInstanceIdentifier(version: 0);

            List<DicomInstanceIdentifier> list = new List<DicomInstanceIdentifier>()
            {
                dicomInstanceId,
                dicomInstanceId2,
            };

            await _dicomMetadataStore.AddInstanceMetadataAsync(dicomDataset);

            _dicomInstanceStore.GetInstanceIdentifiersInStudyAsync(studyInstanceUid, CancellationToken.None).Returns(list);

            DicomRetrieveMetadataRequest request = new DicomRetrieveMetadataRequest(studyInstanceUid);

            DicomRetrieveMetadataResponse response = await _dicomRetrieveMetadataService.GetDicomInstanceMetadataAsync(request);
            Assert.Equal((int)HttpStatusCode.PartialContent, response.StatusCode);
            Assert.Single(response.ResponseMetadata);
            ValidateResponseMetadataDataset(dicomDataset, response.ResponseMetadata.First());
        }

        [Fact]
        public async Task GivenAStudyInstanceUid_WhenRetireveInstanceMetadataFailsToRetrieveAny_ThenNotFoundIsThrown()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid1 = TestUidGenerator.Generate();
            string sopInstanceUid2 = TestUidGenerator.Generate();

            DicomDataset dicomDataset = CreateValidMetadataDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid1);
            var dicomInstanceId = dicomDataset.ToVersionedDicomInstanceIdentifier(version: 0);

            DicomDataset dicomDataset1 = CreateValidMetadataDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid2);
            var dicomInstanceId2 = dicomDataset1.ToVersionedDicomInstanceIdentifier(version: 0);

            List<DicomInstanceIdentifier> list = new List<DicomInstanceIdentifier>()
            {
                dicomInstanceId,
                dicomInstanceId2,
            };

            _dicomInstanceStore.GetInstanceIdentifiersInStudyAsync(studyInstanceUid, CancellationToken.None).Returns(list);

            DicomRetrieveMetadataRequest request = new DicomRetrieveMetadataRequest(studyInstanceUid);

            await Assert.ThrowsAsync<DicomInstanceMetadataNotFoundException>(() => _dicomRetrieveMetadataService.GetDicomInstanceMetadataAsync(request));
        }

        [Fact]
        public async Task GivenAStudyInstanceUid_WhenRetireveInstanceMetadataIsSuccessFull_ThenInstanceMetaDataIsRetrievedSuccessfully()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid1 = TestUidGenerator.Generate();
            string sopInstanceUid2 = TestUidGenerator.Generate();

            DicomDataset dicomDataset = CreateValidMetadataDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid1);
            var dicomInstanceId = dicomDataset.ToVersionedDicomInstanceIdentifier(version: 0);
            await _dicomMetadataStore.AddInstanceMetadataAsync(dicomDataset);

            DicomDataset dicomDataset1 = CreateValidMetadataDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid2);
            var dicomInstanceId2 = dicomDataset1.ToVersionedDicomInstanceIdentifier(version: 0);
            await _dicomMetadataStore.AddInstanceMetadataAsync(dicomDataset1);

            List<DicomInstanceIdentifier> list = new List<DicomInstanceIdentifier>()
            {
                dicomInstanceId,
                dicomInstanceId2,
            };

            _dicomInstanceStore.GetInstanceIdentifiersInStudyAsync(studyInstanceUid, CancellationToken.None).Returns(list);

            DicomRetrieveMetadataRequest request = new DicomRetrieveMetadataRequest(studyInstanceUid);

            DicomRetrieveMetadataResponse response = await _dicomRetrieveMetadataService.GetDicomInstanceMetadataAsync(request);
            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(list.Count, response.ResponseMetadata.Count());

            ValidateResponseMetadataDataset(dicomDataset, response.ResponseMetadata.First());
            ValidateResponseMetadataDataset(dicomDataset1, response.ResponseMetadata.Last());
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
            // Trim the stored dataset to the expected items in the repsonse metadata dataset
            DicomDataset expectedDataset = storedDataset.Clone();
            expectedDataset.RemoveBulkDataVrs();

            // Compare result datasets by serializing.
            var jsonDicomConverter = new JsonDicomConverter();
            Assert.Equal(
                JsonConvert.SerializeObject(expectedDataset, jsonDicomConverter),
                JsonConvert.SerializeObject(retrievedDataset, jsonDicomConverter));
        }
    }
}
