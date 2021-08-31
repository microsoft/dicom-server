// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation
{
    public class ValidationExceptionTests
    {
        [Fact]
        public void GivenDateIsInvalidException_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var value = "tagvalue";
            var exception = new DateIsInvalidException(name, value);
            Assert.Equal($"Dicom element '{name}' with value '{value}' failed validation for VR 'DA': Value cannot be parsed as a valid date.", exception.Message);
        }

        [Fact]
        public void GivenExceedMaxLengthException_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var value = "tagvalue";
            var vr = DicomVR.DA;
            int maxLength = 8;
            var exception = new ExceedMaxLengthException(name, vr, value, maxLength);
            Assert.Equal($"Dicom element '{name}' with value '{value}' failed validation for VR '{vr}': Value length exceeds maximum length of {maxLength}.", exception.Message);
        }

        [Fact]
        public void GivenInvalidCharactersException_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var value = "tagvalue";
            var vr = DicomVR.DA;
            var exception = new InvalidCharactersException(name, vr, value);
            Assert.Equal($"Dicom element '{name}' with value '{value}' failed validation for VR '{vr}': Value contains invalid character.", exception.Message);
        }

        [Fact]
        public void GivenMultiValuesException_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var vr = DicomVR.DA;
            var exception = new MultiValuesException(name, vr);
            Assert.Equal($"Dicom element '{name}' failed validation for VR '{vr}': Dicom element has multiple values. Indexing is only supported on single value element.", exception.Message);
        }

        [Fact]
        public void GivenUnexpectedLengthExceptionWithoutValue_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var vr = DicomVR.DA;
            var expectedLength = 8;
            var exception = new UnexpectedLengthException(name, vr, expectedLength);
            Assert.Equal($"Dicom element '{name}' failed validation for VR '{vr}': Value length is not {expectedLength}.", exception.Message);
        }

        [Fact]
        public void GivenUnexpectedLengthExceptionWithValue_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var value = "tagvalue";
            var vr = DicomVR.DA;
            var expectedLength = 8;
            var exception = new UnexpectedLengthException(name, vr, value, expectedLength);
            Assert.Equal($"Dicom element '{name}' with value '{value}' failed validation for VR '{vr}': Value length is not {expectedLength}.", exception.Message);
        }

        [Fact]
        public void GivenUnexpectedVRException_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var actualVR = DicomVR.DA;
            var expectedVR = DicomVR.DT;
            var exception = new UnexpectedVRException(name, actualVR, expectedVR);
            Assert.Equal($"Dicom element '{name}' failed validation for VR '{actualVR}': The extended query tag '{name}' is expected to have VR '{expectedVR}' but has '{actualVR}' in file.", exception.Message);
        }

        [Fact]
        public void GivenPersonNameExceedMaxComponentsException_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var value = "tagvalue";
            var exception = new PersonNameExceedMaxComponentsException(name, value);
            Assert.Equal($"Dicom element '{name}' with value '{value}' failed validation for VR 'PN': Value contains more than 5 components.", exception.Message);
        }

        [Fact]
        public void GivenPersonNameExceedMaxGroupsException_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var value = "tagvalue";
            var exception = new PersonNameExceedMaxGroupsException(name, value);
            Assert.Equal($"Dicom element '{name}' with value '{value}' failed validation for VR 'PN': Value contains more than 3 groups.", exception.Message);
        }

        [Fact]
        public void GivenPersonNameGroupExceedMaxLengthException_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var value = "tagvalue";
            var exception = new PersonNameGroupExceedMaxLengthException(name, value);
            Assert.Equal($"Dicom element '{name}' with value '{value}' failed validation for VR 'PN': One or more group of person name exceeds maxium length of 64.", exception.Message);
        }

        [Fact]
        public void GivenUidIsInValidException_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var value = "tagvalue";
            var exception = new UidIsInValidException(name, value);
            Assert.Equal($"Dicom element '{name}' with value '{value}' failed validation for VR 'UI': DICOM Identifier is invalid. Value length should not exceed the maximum length of 64 characters. Value should contain characters in '0'-'9' and '.'. Each component must start with non-zero number.", exception.Message);
        }
    }
}
