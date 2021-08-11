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
    public class DicomDateValidationTests
    {
        private readonly DateValidation _validation = new DicomDateValidation();

        [Theory]
        [InlineData("20100141")]
        [InlineData("233434343")]
        public void GivenDAInvalidValue_WhenValidating_ThenShouldThrows(string value)
        {
            DicomDate element = new DicomDate(DicomTag.Date, value);
            Assert.Throws<DicomElementValidationException>(() => _validation.Validate(element));
        }

        [Theory]
        [InlineData("20210313")]
        public void GivenDAValidateValue_WhenValidating_ThenShouldPass(string value)
        {
            DicomDate element = new DicomDate(DicomTag.Date, value);
            _validation.Validate(element);
        }
    }
}
