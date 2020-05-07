// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve
{
    public class RetrieveMetadataHandlerTests
    {
        private readonly IRetrieveMetadataService _retrieveMetadataService;
        private readonly RetrieveMetadataHandler _retrieveMetadataHandler;

        public RetrieveMetadataHandlerTests()
        {
            _retrieveMetadataService = Substitute.For<IRetrieveMetadataService>();
            _retrieveMetadataHandler = new RetrieveMetadataHandler(_retrieveMetadataService);
        }

        [Theory]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("345%^&")]
        [InlineData("()")]
        public async Task GivenARequestWithInvalidStudyInstanceIdentifier_WhenHandlerIsExecuted_ThenDicomInvalidIdentifierExceptionIsThrown(string studyInstanceUid)
        {
            RetrieveMetadataRequest request = new RetrieveMetadataRequest(studyInstanceUid);
            var ex = await Assert.ThrowsAsync<InvalidIdentifierException>(() => _retrieveMetadataHandler.Handle(request, CancellationToken.None));

            Assert.Equal($"DICOM Identifier 'StudyInstanceUid' value '{studyInstanceUid.Trim()}' is invalid. Value length should not exceed the maximum length of 64 characters. Value should contain characters in '0'-'9' and '.'. Each component must start with non-zero number.", ex.Message);
        }

        [Theory]
        [InlineData("aaaa-bbbb", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("aaaa-bbbb", " ")]
        [InlineData("aaaa-bbbb", "345%^&")]
        [InlineData("aaaa-bbbb", "aaaa-bbbb")]
        public async Task GivenARequestWithInvalidStudyIdentifier_WhenRetrievingSeriesMetadata_ThenDicomInvalidIdentifierExceptionIsThrown(string studyInstanceUid, string seriesInstanceUid)
        {
            RetrieveMetadataRequest request = new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid);
            var ex = await Assert.ThrowsAsync<InvalidIdentifierException>(() => _retrieveMetadataHandler.Handle(request, CancellationToken.None));

            Assert.Equal($"DICOM Identifier 'StudyInstanceUid' value '{studyInstanceUid.Trim()}' is invalid. Value length should not exceed the maximum length of 64 characters. Value should contain characters in '0'-'9' and '.'. Each component must start with non-zero number.", ex.Message);
        }

        [Theory]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("345%^&")]
        [InlineData("aaaa-bbbb")]
        [InlineData("()")]
        public async Task GivenARequestWithInvalidSeriesIdentifier_WhenRetrievingSeriesMetadata_ThenDicomInvalidIdentifierExceptionIsThrown(string seriesInstanceUid)
        {
            RetrieveMetadataRequest request = new RetrieveMetadataRequest(TestUidGenerator.Generate(), seriesInstanceUid);
            var ex = await Assert.ThrowsAsync<InvalidIdentifierException>(() => _retrieveMetadataHandler.Handle(request, CancellationToken.None));

            Assert.Equal($"DICOM Identifier 'SeriesInstanceUid' value '{seriesInstanceUid.Trim()}' is invalid. Value length should not exceed the maximum length of 64 characters. Value should contain characters in '0'-'9' and '.'. Each component must start with non-zero number.", ex.Message);
        }

        [Theory]
        [InlineData("aaaa-bbbb1", "aaaa-bbbb2", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("aaaa-bbbb1", "aaaa-bbbb2", "345%^&")]
        [InlineData("aaaa-bbbb1", "aaaa-bbbb2", "aaaa-bbbb2")]
        [InlineData("aaaa-bbbb1", "aaaa-bbbb2", "aaaa-bbbb1")]
        public async Task GivenARequestWithInvalidStudyAndSeriesInstanceIdentifier_WhenRetrievingInstanceMetadata_ThenDicomInvalidIdentifierExceptionIsThrown(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            RetrieveMetadataRequest request = new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            var ex = await Assert.ThrowsAsync<InvalidIdentifierException>(() => _retrieveMetadataHandler.Handle(request, CancellationToken.None));

            Assert.Equal($"DICOM Identifier 'StudyInstanceUid' value '{studyInstanceUid.Trim()}' is invalid. Value length should not exceed the maximum length of 64 characters. Value should contain characters in '0'-'9' and '.'. Each component must start with non-zero number.", ex.Message);
        }

        [Theory]
        [InlineData("aaaa-bbbb2", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("aaaa-bbbb2", "345%^&")]
        [InlineData("aaaa-bbbb2", "aaaa-bbbb2")]
        [InlineData("aaaa-bbbb2", " ")]
        public async Task GivenARequestWithInvalidSeriesInstanceIdentifier_WhenRetrievingInstanceMetadata_ThenDicomInvalidIdentifierExceptionIsThrown(string seriesInstanceUid, string sopInstanceUid)
        {
            RetrieveMetadataRequest request = new RetrieveMetadataRequest(TestUidGenerator.Generate(), seriesInstanceUid, sopInstanceUid);
            var ex = await Assert.ThrowsAsync<InvalidIdentifierException>(() => _retrieveMetadataHandler.Handle(request, CancellationToken.None));

            Assert.Equal($"DICOM Identifier 'SeriesInstanceUid' value '{seriesInstanceUid.Trim()}' is invalid. Value length should not exceed the maximum length of 64 characters. Value should contain characters in '0'-'9' and '.'. Each component must start with non-zero number.", ex.Message);
        }

        [Theory]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("345%^&")]
        [InlineData("aaaa-bbbb")]
        [InlineData("()")]
        public async Task GivenARequestWithInvalidSopInstanceIdentifier_WhenRetrievingInstanceMetadata_ThenDicomInvalidIdentifierExceptionIsThrown(string sopInstanceUid)
        {
            RetrieveMetadataRequest request = new RetrieveMetadataRequest(TestUidGenerator.Generate(), TestUidGenerator.Generate(), sopInstanceUid);
            var ex = await Assert.ThrowsAsync<InvalidIdentifierException>(() => _retrieveMetadataHandler.Handle(request, CancellationToken.None));

            Assert.Equal($"DICOM Identifier 'SopInstanceUid' value '{sopInstanceUid.Trim()}' is invalid. Value length should not exceed the maximum length of 64 characters. Value should contain characters in '0'-'9' and '.'. Each component must start with non-zero number.", ex.Message);
        }

        [Theory]
        [InlineData("1", "1", "2")]
        [InlineData("1", "2", "1")]
        [InlineData("1", "2", "2")]
        public async Task GivenRepeatedIdentifiers_WhenRetrievingInstanceMetadata_ThenDicomBadRequestExceptionIsThrownAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            const string expectedErrorMessage = "The values for StudyInstanceUID, SeriesInstanceUID, SOPInstanceUID must be unique.";
            var request = new RetrieveMetadataRequest(
                studyInstanceUid: studyInstanceUid,
                seriesInstanceUid: seriesInstanceUid,
                sopInstanceUid: sopInstanceUid);

            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _retrieveMetadataHandler.Handle(request, CancellationToken.None));

            Assert.Equal(expectedErrorMessage, ex.Message);
        }

        [Fact]
        public async Task GivenARequestWithValidInstanceIdentifier_WhenRetrievingStudyInstanceMetadata_ThenResponseMetadataIsReturnedSuccessfully()
        {
            string studyInstanceUid = TestUidGenerator.Generate();

            RetrieveMetadataResponse response = SetupRetrieveMetadataResponse();
            _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid).Returns(response);

            RetrieveMetadataRequest request = new RetrieveMetadataRequest(studyInstanceUid);
            await ValidateRetrieveMetadataResponse(response, request);
        }

        [Fact]
        public async Task GivenARequestWithValidInstanceIdentifier_WhenRetrievingSeriesInstanceMetadata_ThenResponseMetadataIsReturnedSuccessfully()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();

            RetrieveMetadataResponse response = SetupRetrieveMetadataResponse();
            _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid).Returns(response);

            RetrieveMetadataRequest request = new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid);
            await ValidateRetrieveMetadataResponse(response, request);
        }

        [Fact]
        public async Task GivenARequestWithValidInstanceIdentifier_WhenRetrievingSopInstanceMetadata_ThenResponseMetadataIsReturnedSuccessfully()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            RetrieveMetadataResponse response = SetupRetrieveMetadataResponse();
            _retrieveMetadataService.RetrieveSopInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid).Returns(response);

            RetrieveMetadataRequest request = new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            await ValidateRetrieveMetadataResponse(response, request);
        }

        private static RetrieveMetadataResponse SetupRetrieveMetadataResponse()
        {
            return new RetrieveMetadataResponse(
                new List<DicomDataset> { new DicomDataset() });
        }

        private async Task ValidateRetrieveMetadataResponse(RetrieveMetadataResponse response, RetrieveMetadataRequest request)
        {
            RetrieveMetadataResponse actualResponse = await _retrieveMetadataHandler.Handle(request, CancellationToken.None);
            Assert.Same(response, actualResponse);
        }
    }
}
