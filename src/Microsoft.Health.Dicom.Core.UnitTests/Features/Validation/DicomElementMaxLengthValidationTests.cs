// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation
{
    public class DicomElementMaxLengthValidationTests
    {

        [Fact]
        public void GivenValueExceedMaxLength_WhenValidating_ThenShouldThrows()
        {
            Assert.Throws<DicomElementValidationException>(() =>
                new DicomElementMaxLengthValidation(12).Validate(new DicomIntegerString(DicomTag.DoseReferenceNumber, "0123456789121")));
        }

        [Fact]
        public void GivenValueInRange_WhenValidating_ThenShouldPass()
        {
            new DicomElementMaxLengthValidation(12).Validate(new DicomIntegerString(DicomTag.DoseReferenceNumber, "012345678912"));
        }
    }
}
