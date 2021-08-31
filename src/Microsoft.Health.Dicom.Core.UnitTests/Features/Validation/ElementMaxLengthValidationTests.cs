// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions.Validation;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation
{
    public class ElementMaxLengthValidationTests
    {

        [Fact]
        public void GivenValueExceedMaxLength_WhenValidating_ThenShouldThrows()
        {
            Assert.Throws<ExceedMaxLengthException>(() =>
                new ElementMaxLengthValidation(12).Validate(new DicomIntegerString(DicomTag.DoseReferenceNumber, "0123456789121")));
        }

        [Fact]
        public void GivenValueInRange_WhenValidating_ThenShouldPass()
        {
            new ElementMaxLengthValidation(12).Validate(new DicomIntegerString(DicomTag.DoseReferenceNumber, "012345678912"));
        }
    }
}
