// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Dicom.IO;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation
{
    public class ElementRequiredLengthValidationTests
    {

        [Fact]
        public void GivenBinaryValueNotRequiredLength_WhenValidating_ThenShouldThrows()
        {
            DicomElement element = new DicomSignedShort(DicomTag.LargestImagePixelValue, ByteConverter.ToByteBuffer(new int[] { int.MaxValue }));
            Assert.Throws<DicomElementValidationException>(() =>
              new ElementRequiredLengthValidation(4).Validate(element));
        }

        [Fact]
        public void GivenStringValueNotRequiredLength_WhenValidating_ThenShouldThrows()
        {
            DicomElement element = new DicomAgeString(DicomTag.PatientAge, "012W1");
            Assert.Throws<DicomElementValidationException>(() =>
              new ElementRequiredLengthValidation(4).Validate(element));
        }

        [Fact]
        public void GivenBinaryValueOfRequiredLength_WhenValidating_ThenShouldPass()
        {
            DicomElement element = new DicomSignedShort(DicomTag.LargestImagePixelValue, short.MaxValue);
            new ElementRequiredLengthValidation(2).Validate(element);
        }

        [Fact]
        public void GivenStringValueOfRequiredLength_WhenValidating_ThenShouldThrows()
        {
            DicomElement element = new DicomAgeString(DicomTag.PatientAge, "012W");
            new ElementRequiredLengthValidation(4).Validate(element);
        }
    }
}
