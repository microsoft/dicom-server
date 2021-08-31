// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions.Validation;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation
{
    public class PersonNameValidationTests
    {

        [Fact]
        public void GivenValidatePersonName_WhenValidating_ThenShouldPass()
        {
            DicomElement element = new DicomPersonName(DicomTag.PatientName, "abc^xyz=abc^xyz^xyz^xyz^xyz=abc^xyz");
            new PersonNameValidation().Validate(element);
        }

        [Theory]
        [InlineData("abc^xyz=abc^xyz=abc^xyz=abc^xyz", typeof(PersonNameExceedMaxGroupsException))] // too many groups (>3)
        [InlineData("abc^efg^hij^pqr^lmn^xyz", typeof(PersonNameExceedMaxComponentsException))]  // to many group components
        [InlineData("0123456789012345678901234567890123456789012345678901234567890123456789", typeof(PersonNameGroupExceedMaxLengthException))]  // group is too long
        public void GivenInvalidPatientName_WhenValidating_ThenShouldThrow(string value, Type exceptionType)
        {
            DicomElement element = new DicomPersonName(DicomTag.PatientName, value);
            Assert.Throws(exceptionType, () => new PersonNameValidation().Validate(element));

        }

    }
}
