// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Messages.Retrieve
{
    public class RetrieveRequestValidatorTests
    {
        [Theory]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "Dicom element 'StudyInstanceUid' with value 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa...' failed validation for VR 'UI': Dicom Identifier exceeds max length.")]
        [InlineData("345%^&",
            "Dicom element 'StudyInstanceUid' with value '345%^&' failed validation for VR 'UI': Dicom Identifier should only contain characters in '0'-'9' and '.', and each component must start with non-zero number.")]
        public void GivenAnInvalidStudyInstanceIdentifier_WhenValidatedForRequestedResourceTypeStudy_ThenInvalidIdentifierExceptionIsThrown(string studyInstanceUid, string expectedMessage)
        {
            EnsureArg.IsNotNull(studyInstanceUid, nameof(studyInstanceUid));
            var ex = Assert.Throws<InvalidIdentifierException>(() => RetrieveRequestValidator.ValidateInstanceIdentifiers(ResourceType.Study, studyInstanceUid));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "Dicom element 'SeriesInstanceUid' with value 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa...' failed validation for VR 'UI': Dicom Identifier exceeds max length.")]
        [InlineData("345%^&",
            "Dicom element 'SeriesInstanceUid' with value '345%^&' failed validation for VR 'UI': Dicom Identifier should only contain characters in '0'-'9' and '.', and each component must start with non-zero number.")]
        [InlineData("aaaa-bbbb",
            "Dicom element 'SeriesInstanceUid' with value 'aaaa-bbbb' failed validation for VR 'UI': Dicom Identifier should only contain characters in '0'-'9' and '.', and each component must start with non-zero number.")]
        [InlineData("()",
            "Dicom element 'SeriesInstanceUid' with value '()' failed validation for VR 'UI': Dicom Identifier should only contain characters in '0'-'9' and '.', and each component must start with non-zero number.")]
        public void GivenAnInvalidSeriesInstanceIdentifier_WhenValidatedForRequestedResourceTypeSeries_ThenInvalidIdentifierExceptionIsThrown(string seriesInstanceUid, string expectedMessage)
        {
            EnsureArg.IsNotNull(seriesInstanceUid, nameof(seriesInstanceUid));
            var ex = Assert.Throws<InvalidIdentifierException>(() => RetrieveRequestValidator.ValidateInstanceIdentifiers(ResourceType.Series, TestUidGenerator.Generate(), seriesInstanceUid));
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

        public void GivenAnInvalidInstanceIdentifier_WhenValidatedForRequestedResourceTypeInstance_ThenInvalidIdentifierExceptionIsThrown(string sopInstanceUid, string expectedMessage)
        {
            EnsureArg.IsNotNull(sopInstanceUid, nameof(sopInstanceUid));
            var ex = Assert.Throws<InvalidIdentifierException>(() => RetrieveRequestValidator.ValidateInstanceIdentifiers(ResourceType.Instance, TestUidGenerator.Generate(), TestUidGenerator.Generate(), sopInstanceUid));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData("1", "1", "2")]
        [InlineData("1", "2", "1")]
        [InlineData("1", "2", "2")]
        public void GivenARequestWithRepeatedIdentifiers_WhenValidatedForRequestedResourceTypeInstance_ThenBadRequestExceptionIsThrown(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            var ex = Assert.Throws<BadRequestException>(() => RetrieveRequestValidator.ValidateInstanceIdentifiers(ResourceType.Instance, studyInstanceUid, seriesInstanceUid, sopInstanceUid));
            Assert.Equal("The values for StudyInstanceUID, SeriesInstanceUID, SOPInstanceUID must be unique.", ex.Message);
        }

        [Fact]
        public void GivenARequestWithRepeatedStudyAndSeriesInstanceIdentifiers_WhenValidatedForRequestedResourceTypeSeries_ThenBadRequestExceptionIsThrown()
        {
            string studyInstanceUid = TestUidGenerator.Generate();

            // Use same identifier as studyInstanceUid and seriesInstanceUid.
            var ex = Assert.Throws<BadRequestException>(() => RetrieveRequestValidator.ValidateInstanceIdentifiers(ResourceType.Series, studyInstanceUid, studyInstanceUid));
            Assert.Equal("The values for StudyInstanceUID, SeriesInstanceUID, SOPInstanceUID must be unique.", ex.Message);
        }

        [Theory]
        [InlineData("*-")]
        [InlineData("invalid")]
        [InlineData("00000000000000000000000000000000000000000000000000000000000000065")]
        public void GivenARequestWithIncorrectTransferSyntax_WhenValidated_ThenBadRequestExceptionIsThrown(string transferSyntax)
        {
            var ex = Assert.Throws<BadRequestException>(() => RetrieveRequestValidator.ValidateTransferSyntax(requestedTransferSyntax: transferSyntax));
            Assert.Equal("The specified Transfer Syntax value is not valid.", ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new int[0])]
        public void GivenARequestWithNoFrames_WhenValidated_ThenBadRequestExceptionIsThrown(int[] frames)
        {
            string expectedErrorMessage = "The specified frames value is not valid. At least one frame must be present, and all requested frames must have value greater than 0.";

            var ex = Assert.Throws<BadRequestException>(() => RetrieveRequestValidator.ValidateFrames(frames));
            Assert.Equal(expectedErrorMessage, ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-234)]
        public void GivenARequestWithInvalidFrameNumber_WhenValidated_ThenBadRequestExceptionIsThrown(int frame)
        {
            string expectedErrorMessage = "The specified frames value is not valid. At least one frame must be present, and all requested frames must have value greater than 0.";

            var ex = Assert.Throws<BadRequestException>(() => RetrieveRequestValidator.ValidateFrames(new[] { frame }));

            Assert.Equal(expectedErrorMessage, ex.Message);
        }

        [Theory]
        [InlineData(ResourceType.Study)]
        [InlineData(ResourceType.Series)]
        [InlineData(ResourceType.Instance)]
        public void GivenARequestWithValidInstanceIdentifiers__WhenValidatedForSpecifiedResourceType_ThenNoExceptionIsThrown(ResourceType resourceType)
        {
            RetrieveRequestValidator.ValidateInstanceIdentifiers(resourceType, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate());
        }

        [Fact]
        public void GivenARequestWithValidFramesValue_WhenValidated_ThenNoExceptionIsThrown()
        {
            RetrieveRequestValidator.ValidateFrames(new List<int> { 1 });
        }

        [Fact]
        public void GivenARequestWithValidTransferSyntax_WhenValidated_ThenNoExceptionIsThrown()
        {
            RetrieveRequestValidator.ValidateTransferSyntax(DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);
        }

        [Fact]
        public void GivenARequestWithOriginalTransferSyntax_WhenValidated_ThenNoExceptionIsThrown()
        {
            RetrieveRequestValidator.ValidateTransferSyntax(requestedTransferSyntax: "*", originalTransferSyntaxRequested: true);
        }
    }
}
