// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve
{
    public class DicomRetrieveMetadataHandlerTests
    {
        private readonly IDicomRetrieveMetadataService _dicomRetrieveMetadataService;
        private readonly DicomRetrieveMetadataHandler _dicomRetrieveMetadataHandler;

        public DicomRetrieveMetadataHandlerTests()
        {
            _dicomRetrieveMetadataService = Substitute.For<IDicomRetrieveMetadataService>();
            _dicomRetrieveMetadataHandler = new DicomRetrieveMetadataHandler(_dicomRetrieveMetadataService);
        }

        [Fact]
        public async Task GivenARequestWithValidInstanceIdentifier_WhenRetrievingStudyInstanceMetadata_ThenResponseMetadataIsReturnedSuccessfully()
        {
            string studyInstanceUid = TestUidGenerator.Generate();

            DicomRetrieveMetadataResponse response = SetupRetrieveMetadataResponse();
            _dicomRetrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid).Returns(response);

            DicomRetrieveMetadataRequest request = new DicomRetrieveMetadataRequest(studyInstanceUid);
            await ValidateRetrieveMetadataResponse(response, request);
        }

        [Fact]
        public async Task GivenARequestWithValidInstanceIdentifier_WhenRetrievingSeriesInstanceMetadata_ThenResponseMetadataIsReturnedSuccessfully()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();

            DicomRetrieveMetadataResponse response = SetupRetrieveMetadataResponse();
            _dicomRetrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid).Returns(response);

            DicomRetrieveMetadataRequest request = new DicomRetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid);
            await ValidateRetrieveMetadataResponse(response, request);
        }

        [Fact]
        public async Task GivenARequestWithValidInstanceIdentifier_WhenRetrievingSopInstanceMetadata_ThenResponseMetadataIsReturnedSuccessfully()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            DicomRetrieveMetadataResponse response = SetupRetrieveMetadataResponse();
            _dicomRetrieveMetadataService.RetrieveSopInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid).Returns(response);

            DicomRetrieveMetadataRequest request = new DicomRetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            await ValidateRetrieveMetadataResponse(response, request);
        }

        private static DicomRetrieveMetadataResponse SetupRetrieveMetadataResponse()
        {
            return new DicomRetrieveMetadataResponse(
                HttpStatusCode.OK,
                new List<DicomDataset> { new DicomDataset() });
        }

        private async Task ValidateRetrieveMetadataResponse(DicomRetrieveMetadataResponse response, DicomRetrieveMetadataRequest request)
        {
            DicomRetrieveMetadataResponse actualResponse = await _dicomRetrieveMetadataHandler.Handle(request, CancellationToken.None);
            Assert.Same(response, actualResponse);
        }
    }
}
