// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve
{
    public class DicomRetrieveResourceHandlerTests
    {
        private readonly IDicomRetrieveResourceService _dicomRetrieveResourceService;
        private readonly DicomRetrieveResourceHandler _dicomRetrieveResourceHandler;

        public DicomRetrieveResourceHandlerTests()
        {
            _dicomRetrieveResourceService = Substitute.For<IDicomRetrieveResourceService>();
            _dicomRetrieveResourceHandler = new DicomRetrieveResourceHandler(_dicomRetrieveResourceService);
        }

        [Theory]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("345%^&")]
        public async Task GivenARequestWithInvalidIdentifier_WhenRetrievingStudy_ThenDicomInvalidIdentifierExceptionIsThrown(string studyInstanceUid)
        {
            DicomRetrieveResourceRequest request = new DicomRetrieveResourceRequest("*", studyInstanceUid);
            var ex = await Assert.ThrowsAsync<DicomInvalidIdentifierException>(() => _dicomRetrieveResourceHandler.Handle(request, CancellationToken.None));

            Assert.Equal($"Dicom Identifier 'StudyInstanceUid' value '{studyInstanceUid.Trim()}' is invalid. Value length should not exceed the maximum length of 64 characters. Value should contain characters in '0'-'9' and '.'. Each component must start with non-zero number.", ex.Message);
        }

        [Theory]
        [InlineData("aaaa-bbbb", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("aaaa-bbbb", " ")]
        [InlineData("aaaa-bbbb", "345%^&")]
        [InlineData("aaaa-bbbb", "aaaa-bbbb")]
        public async Task GivenARequestWithInvalidStudyAndSeriesIdentifiers_WhenRetrievingSeries_ThenDicomInvalidIdentifierExceptionIsThrown(string studyInstanceUid, string seriesInstanceUid)
        {
            DicomRetrieveResourceRequest request = new DicomRetrieveResourceRequest("*", studyInstanceUid, seriesInstanceUid);
            var ex = await Assert.ThrowsAsync<DicomInvalidIdentifierException>(() => _dicomRetrieveResourceHandler.Handle(request, CancellationToken.None));

            Assert.Equal($"Dicom Identifier 'StudyInstanceUid' value '{studyInstanceUid.Trim()}' is invalid. Value length should not exceed the maximum length of 64 characters. Value should contain characters in '0'-'9' and '.'. Each component must start with non-zero number.", ex.Message);
        }

        [Theory]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("345%^&")]
        [InlineData("aaaa-bbbb")]
        [InlineData("()")]
        public async Task GivenARequestWithInvalidSeriesIdentifier_WhenRetrievingSeries_ThenDicomInvalidIdentifierExceptionIsThrown(string seriesInstanceUid)
        {
            DicomRetrieveResourceRequest request = new DicomRetrieveResourceRequest("*", TestUidGenerator.Generate(), seriesInstanceUid);
            var ex = await Assert.ThrowsAsync<DicomInvalidIdentifierException>(() => _dicomRetrieveResourceHandler.Handle(request, CancellationToken.None));

            Assert.Equal($"Dicom Identifier 'SeriesInstanceUid' value '{seriesInstanceUid.Trim()}' is invalid. Value length should not exceed the maximum length of 64 characters. Value should contain characters in '0'-'9' and '.'. Each component must start with non-zero number.", ex.Message);
        }

        [Theory]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("345%^&")]
        [InlineData("aaaa-bbbb")]
        [InlineData("()")]
        public async Task GivenARequestWithInvalidInstanceIdentifier_WhenRetrievingInstance_ThenDicomInvalidIdentifierExceptionIsThrown(string sopInstanceUid)
        {
            DicomRetrieveResourceRequest request = new DicomRetrieveResourceRequest("*", TestUidGenerator.Generate(), TestUidGenerator.Generate(), sopInstanceUid);
            var ex = await Assert.ThrowsAsync<DicomInvalidIdentifierException>(() => _dicomRetrieveResourceHandler.Handle(request, CancellationToken.None));

            Assert.Equal($"Dicom Identifier 'SopInstanceUid' value '{sopInstanceUid.Trim()}' is invalid. Value length should not exceed the maximum length of 64 characters. Value should contain characters in '0'-'9' and '.'. Each component must start with non-zero number.", ex.Message);
        }

        [Theory]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("345%^&")]
        [InlineData("aaaa-bbbb")]
        [InlineData("()")]
        public async Task GivenARequestWithInvalidInstanceIdentifier_WhenRetrievingFrames_TheServerShouldReturnBadRequest(string sopInstanceUid)
        {
            DicomRetrieveResourceRequest request = new DicomRetrieveResourceRequest("*", TestUidGenerator.Generate(), TestUidGenerator.Generate(), sopInstanceUid, new List<int> { 1 });
            var ex = await Assert.ThrowsAsync<DicomInvalidIdentifierException>(() => _dicomRetrieveResourceHandler.Handle(request, CancellationToken.None));

            Assert.Equal($"Dicom Identifier 'SopInstanceUid' value '{sopInstanceUid.Trim()}' is invalid. Value length should not exceed the maximum length of 64 characters. Value should contain characters in '0'-'9' and '.'. Each component must start with non-zero number.", ex.Message);
        }
    }
}
