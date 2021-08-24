// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve
{
    public class RetrieveResourceHandlerTests
    {
        private readonly IRetrieveResourceService _retrieveResourceService;
        private readonly RetrieveResourceHandler _retrieveResourceHandler;

        public RetrieveResourceHandlerTests()
        {
            _retrieveResourceService = Substitute.For<IRetrieveResourceService>();
            _retrieveResourceHandler = new RetrieveResourceHandler(new DisabledAuthorizationService<DataActions>(), _retrieveResourceService);
        }

        [Theory]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "Dicom element 'StudyInstanceUid' with value 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa...' failed validation for VR 'UI': Dicom Identifier exceeds max length.")]
        [InlineData("345%^&",
            "Dicom element 'StudyInstanceUid' with value '345%^&' failed validation for VR 'UI': Dicom Identifier should only contain characters in '0'-'9' and '.', and each component must start with non-zero number.")]
        public async Task GivenARequestWithInvalidIdentifier_WhenRetrievingStudy_ThenDicomInvalidIdentifierExceptionIsThrown(string studyInstanceUid, string expectedMessage)
        {
            EnsureArg.IsNotNull(studyInstanceUid, nameof(studyInstanceUid));
            RetrieveResourceRequest request = new RetrieveResourceRequest(studyInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetInstance() });
            var ex = await Assert.ThrowsAsync<InvalidIdentifierException>(() => _retrieveResourceHandler.Handle(request, CancellationToken.None));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData("aaaa-bbbb", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("aaaa-bbbb", " ")]
        [InlineData("aaaa-bbbb", "345%^&")]
        [InlineData("aaaa-bbbb", "aaaa-bbbb")]
        public async Task GivenARequestWithInvalidStudyAndSeriesIdentifiers_WhenRetrievingSeries_ThenDicomInvalidIdentifierExceptionIsThrown(string studyInstanceUid, string seriesInstanceUid)
        {
            EnsureArg.IsNotNull(studyInstanceUid, nameof(studyInstanceUid));
            RetrieveResourceRequest request = new RetrieveResourceRequest(studyInstanceUid, seriesInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetSeries() });
            var ex = await Assert.ThrowsAsync<InvalidIdentifierException>(() => _retrieveResourceHandler.Handle(request, CancellationToken.None));
            Assert.Equal($"Dicom element 'StudyInstanceUid' with value '{studyInstanceUid.Truncate(64)}' failed validation for VR 'UI': Dicom Identifier should only contain characters in '0'-'9' and '.', and each component must start with non-zero number.", ex.Message);
        }

        [Theory]
        [InlineData(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "Dicom element 'SeriesInstanceUid' with value 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa...' failed validation for VR 'UI': Dicom Identifier exceeds max length.")]
        [InlineData("345%^&",
            "Dicom element 'SeriesInstanceUid' with value '345%^&' failed validation for VR 'UI': Dicom Identifier should only contain characters in '0'-'9' and '.', and each component must start with non-zero number.")]
        [InlineData("aaaa-bbbb",
            "Dicom element 'SeriesInstanceUid' with value 'aaaa-bbbb' failed validation for VR 'UI': Dicom Identifier should only contain characters in '0'-'9' and '.', and each component must start with non-zero number.")]
        [InlineData("()",
            "Dicom element 'SeriesInstanceUid' with value '()' failed validation for VR 'UI': Dicom Identifier should only contain characters in '0'-'9' and '.', and each component must start with non-zero number.")]
        public async Task GivenARequestWithInvalidSeriesIdentifier_WhenRetrievingSeries_ThenDicomInvalidIdentifierExceptionIsThrown(string seriesInstanceUid, string expectedMessage)
        {
            EnsureArg.IsNotNull(seriesInstanceUid, nameof(seriesInstanceUid));
            RetrieveResourceRequest request = new RetrieveResourceRequest(TestUidGenerator.Generate(), seriesInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetSeries() });
            var ex = await Assert.ThrowsAsync<InvalidIdentifierException>(() => _retrieveResourceHandler.Handle(request, CancellationToken.None));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "Dicom element 'SopInstanceUid' with value 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa...' failed validation for VR 'UI': Dicom Identifier exceeds max length.")]
        [InlineData("345%^&",
            "Dicom element 'SopInstanceUid' with value '345%^&' failed validation for VR 'UI': Dicom Identifier should only contain characters in '0'-'9' and '.', and each component must start with non-zero number.")]
        [InlineData("aaaa-bbbb",
            "Dicom element 'SopInstanceUid' with value 'aaaa-bbbb' failed validation for VR 'UI': Dicom Identifier should only contain characters in '0'-'9' and '.', and each component must start with non-zero number.")]
        [InlineData("()",
            "Dicom element 'SopInstanceUid' with value '()' failed validation for VR 'UI': Dicom Identifier should only contain characters in '0'-'9' and '.', and each component must start with non-zero number.")]
        public async Task GivenARequestWithInvalidInstanceIdentifier_WhenRetrievingInstance_ThenDicomInvalidIdentifierExceptionIsThrown(string sopInstanceUid, string expectedMessage)
        {
            EnsureArg.IsNotNull(sopInstanceUid, nameof(sopInstanceUid));
            RetrieveResourceRequest request = new RetrieveResourceRequest(TestUidGenerator.Generate(), TestUidGenerator.Generate(), sopInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetInstance() });
            var ex = await Assert.ThrowsAsync<InvalidIdentifierException>(() => _retrieveResourceHandler.Handle(request, CancellationToken.None));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "Dicom element 'SopInstanceUid' with value 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa...' failed validation for VR 'UI': Dicom Identifier exceeds max length.")]
        [InlineData("()", "Dicom element 'SopInstanceUid' with value '()' failed validation for VR 'UI': Dicom Identifier should only contain characters in '0'-'9' and '.', and each component must start with non-zero number.")]
        [InlineData("345%^&", "Dicom element 'SopInstanceUid' with value '345%^&' failed validation for VR 'UI': Dicom Identifier should only contain characters in '0'-'9' and '.', and each component must start with non-zero number.")]
        [InlineData("aaaa-bbbb", "Dicom element 'SopInstanceUid' with value 'aaaa-bbbb' failed validation for VR 'UI': Dicom Identifier should only contain characters in '0'-'9' and '.', and each component must start with non-zero number.")]
        public async Task GivenARequestWithInvalidInstanceIdentifier_WhenRetrievingFrames_ThenDicomInvalidIdentifierExceptionIsThrown(string sopInstanceUid, string expectedMessage)
        {
            EnsureArg.IsNotNull(sopInstanceUid, nameof(sopInstanceUid));
            RetrieveResourceRequest request = new RetrieveResourceRequest(TestUidGenerator.Generate(), TestUidGenerator.Generate(), sopInstanceUid, new List<int> { 1 }, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame() });
            var ex = await Assert.ThrowsAsync<InvalidIdentifierException>(() => _retrieveResourceHandler.Handle(request, CancellationToken.None));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory(Skip = "Move this tests to move this tests to RetriveResourceService, since the logic to validate TransferSyntax has moved there")]
        [InlineData("*-")]
        [InlineData("invalid")]
        [InlineData("00000000000000000000000000000000000000000000000000000000000000065")]
        public async Task GivenIncorrectTransferSyntax_WhenRetrievingStudy_ThenDicomBadRequestExceptionIsThrownAsync(string transferSyntax)
        {
            var request = new RetrieveResourceRequest(TestUidGenerator.Generate(), new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetInstance(transferSyntax: transferSyntax) });

            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _retrieveResourceHandler.Handle(request, CancellationToken.None));

            Assert.Equal("The specified Transfer Syntax value is not valid.", ex.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-234)]
        public async Task GivenInvalidFrameNumber_WhenRetrievingFrames_ThenDicomBadRequestExceptionIsThrownAsync(int frame)
        {
            const string expectedErrorMessage = "The specified frames value is not valid. At least one frame must be present, and all requested frames must have value greater than 0.";
            var request = new RetrieveResourceRequest(
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: TestUidGenerator.Generate(),
                sopInstanceUid: TestUidGenerator.Generate(),
                frames: new[] { frame },
                acceptHeaders: new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame() });

            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _retrieveResourceHandler.Handle(request, CancellationToken.None));

            Assert.Equal(expectedErrorMessage, ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new int[0])]
        public async Task GivenNoFrames_WhenRetrievingFrames_ThenDicomBadRequestExceptionIsThrownAsync(int[] frames)
        {
            const string expectedErrorMessage = "The specified frames value is not valid. At least one frame must be present, and all requested frames must have value greater than 0.";
            var request = new RetrieveResourceRequest(
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: TestUidGenerator.Generate(),
                sopInstanceUid: TestUidGenerator.Generate(),
                frames: frames,
                acceptHeaders: new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame() });

            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _retrieveResourceHandler.Handle(request, CancellationToken.None));

            Assert.Equal(expectedErrorMessage, ex.Message);
        }

        [Theory]
        [InlineData("1", "1", "2")]
        [InlineData("1", "2", "1")]
        [InlineData("1", "2", "2")]
        public async Task GivenRepeatedIdentifiers_WhenRetrievingFrames_ThenDicomBadRequestExceptionIsThrownAsync(
            string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            const string expectedErrorMessage = "The values for StudyInstanceUID, SeriesInstanceUID, SOPInstanceUID must be unique.";
            var request = new RetrieveResourceRequest(
                studyInstanceUid: studyInstanceUid,
                seriesInstanceUid: seriesInstanceUid,
                sopInstanceUid: sopInstanceUid,
                frames: new int[] { 1 },
                acceptHeaders: new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame() });

            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _retrieveResourceHandler.Handle(request, CancellationToken.None));

            Assert.Equal(expectedErrorMessage, ex.Message);
        }

        [Fact]
        public async Task GivenARequestWithValidInstanceIdentifier_WhenRetrievingFrames_ThenNoExceptionIsThrown()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            RetrieveResourceResponse expectedResponse = new RetrieveResourceResponse(Enumerable.Empty<Stream>(), KnownContentTypes.ApplicationOctetStream);
            RetrieveResourceRequest request = new RetrieveResourceRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, new List<int> { 1 }, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame() });
            _retrieveResourceService.GetInstanceResourceAsync(request, CancellationToken.None).Returns(expectedResponse);

            RetrieveResourceResponse response = await _retrieveResourceHandler.Handle(request, CancellationToken.None);
            Assert.Same(expectedResponse, response);
        }
    }
}
