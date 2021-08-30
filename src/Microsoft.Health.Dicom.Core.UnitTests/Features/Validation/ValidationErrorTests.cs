// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Features.Validation.Errors;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation
{
    public class ValidationErrorTests
    {
        [Fact]
        public void GivenDateIsInvalidError_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var value = "tagvalue";
            var error = new DateIsInvalidError(name, value);
            Assert.Equal($"Dicom element '{name}' with value '{value}' failed validation for VR 'DA': Value cannot be parsed as a valid date.", error.Message);
        }

        [Fact]
        public void GivenExceedMaxLengthError_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var value = "tagvalue";
            var vr = DicomVR.DA;
            int maxLength = 8;
            var error = new ExceedMaxLengthError(name, vr, value, maxLength);
            Assert.Equal($"Dicom element '{name}' with value '{value}' failed validation for VR '{vr}': Value length exceeds maximum length of {maxLength}.", error.Message);
        }

        [Fact]
        public void GivenHasInvalidCharactersError_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var value = "tagvalue";
            var vr = DicomVR.DA;
            var error = new HasInvalidCharactersError(name, vr, value);
            Assert.Equal($"Dicom element '{name}' with value '{value}' failed validation for VR '{vr}': Value contains invalid character.", error.Message);
        }

        [Fact]
        public void GivenMultiValuesError_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var vr = DicomVR.DA;
            var error = new MultiValuesError(name, vr);
            Assert.Equal($"Dicom element '{name}' failed validation for VR '{vr}': Dicom element has multiple values. Indexing is only supported on single value element.", error.Message);
        }

        [Fact]
        public void GivenUnexpectedLengthErrorWithoutValue_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var vr = DicomVR.DA;
            var expectedLength = 8;
            var error = new UnexpectedLengthError(name, vr, expectedLength);
            Assert.Equal($"Dicom element '{name}' failed validation for VR '{vr}': Value length is not {expectedLength}.", error.Message);
        }

        [Fact]
        public void GivenUnexpectedLengthErrorWithValue_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var value = "tagvalue";
            var vr = DicomVR.DA;
            var expectedLength = 8;
            var error = new UnexpectedLengthError(name, vr, value, expectedLength);
            Assert.Equal($"Dicom element '{name}' with value '{value}' failed validation for VR '{vr}': Value length is not {expectedLength}.", error.Message);
        }

        [Fact]
        public void GivenUnexpectedVRError_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var actualVR = DicomVR.DA;
            var expectedVR = DicomVR.DT;
            var error = new UnexpectedVRError(name, actualVR, expectedVR);
            Assert.Equal($"Dicom element '{name}' failed validation for VR '{actualVR}': The extended query tag '{name}' is expected to have VR '{expectedVR}' but has '{actualVR}' in file.", error.Message);
        }

        [Fact]
        public void GivenPersonNameExceedMaxComponentsError_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var value = "tagvalue";
            var error = new PersonNameExceedMaxComponentsError(name, value);
            Assert.Equal($"Dicom element '{name}' with value '{value}' failed validation for VR 'PN': Value contains more than 5 components.", error.Message);
        }

        [Fact]
        public void GivenPersonNameExceedMaxGroupsError_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var value = "tagvalue";
            var error = new PersonNameExceedMaxGroupsError(name, value);
            Assert.Equal($"Dicom element '{name}' with value '{value}' failed validation for VR 'PN': Value contains more than 3 groups.", error.Message);
        }

        [Fact]
        public void GivenPersonNameGroupExceedMaxLengthError_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var value = "tagvalue";
            var error = new PersonNameGroupExceedMaxLengthError(name, value);
            Assert.Equal($"Dicom element '{name}' with value '{value}' failed validation for VR 'PN': One or more group of person name exceeds maxium length of 64.", error.Message);
        }

        [Fact]
        public void GivenUidIsInValidError_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var value = "tagvalue";
            var error = new UidIsInValidError(name, value);
            Assert.Equal($"Dicom element '{name}' with value '{value}' failed validation for VR 'UI': DICOM Identifier is invalid. Value length should not exceed the maximum length of 64 characters. Value should contain characters in '0'-'9' and '.'. Each component must start with non-zero number.", error.Message);
        }
    }
}
