// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation;

public class StringElementValidationTests
{
    private class StringValidation : StringElementValidation
    {
        protected override void ValidateStringElement(string name, string value, DicomVR vr, IByteBuffer buffer)
        {
        }

        protected override bool GetValue(DicomElement dicomElement, out string value)
        {
            value = dicomElement.GetFirstValueOrDefault<string>();
            return string.IsNullOrEmpty(value);
        }
    }
    [Theory]
    [InlineData("13.14.520")]
    [InlineData("13")]
    [InlineData("13\0\0\0")]
    [InlineData(null)]
    public void GivenValidateUid_WhenValidating_ThenShouldPass(string value)
    {
        DicomElement element = new DicomUniqueIdentifier(DicomTag.DigitalSignatureUID, value);
        new StringValidation().Validate(element);
    }
}