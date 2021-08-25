// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Exceptions
{
    public class DicomElementValidationExceptionTests
    {

        [Fact]
        public void GivenNoValue_WhenGetMessage_ShouldReturnExpected()
        {
            string name = "test name";
            DicomVR vr = DicomVR.DA;
            string message = "error message";
            DicomElementValidationException ex = new DicomElementValidationException(name, vr, message);
            Assert.Equal($"Dicom element '{name}' failed validation for VR '{vr}': {message}", ex.Message);
        }

        [Fact]
        public void GivenValueNotRequireTruncating_WhenGetMessage_ShouldReturnExpected()
        {
            string name = "test name";
            DicomVR vr = DicomVR.DA;
            string message = "error message";
            string value = "short value";
            DicomElementValidationException ex = new DicomElementValidationException(name, vr, value, value.Length, message);
            Assert.Equal($"Dicom element '{name}' with value '{value}' failed validation for VR '{vr}': {message}", ex.Message);
        }

        [Fact]
        public void GivenValueRequireTruncating_WhenGetMessage_ShouldReturnExpected()
        {
            string name = "test name";
            DicomVR vr = DicomVR.DA;
            string message = "error message";
            string value = "long long value";
            int length = value.Length - 2;
            DicomElementValidationException ex = new DicomElementValidationException(name, vr, value, length, message);
            Assert.Equal($"Dicom element '{name}' with value starting with '{value.Substring(0, length)}' failed validation for VR '{vr}': {message}", ex.Message);
        }
    }
}
