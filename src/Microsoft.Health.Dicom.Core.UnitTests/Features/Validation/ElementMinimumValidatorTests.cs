// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Dicom.IO;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation
{
    public class ElementMinimumValidatorTests
    {
        private readonly IElementMinimumValidator _validator;

        public ElementMinimumValidatorTests()
        {
            _validator = new ElementMinimumValidator();
        }

        [Theory]
        [MemberData(nameof(SupportedDicomElements))]
        public void GivenSupportedVR_WhenValidating_ThenShouldPass(DicomElement dicomElement)
        {
            _validator.Validate(dicomElement);
        }

        public static IEnumerable<object[]> SupportedDicomElements()
        {
            yield return new object[] { new DicomApplicationEntity(DicomTag.DestinationAE, "012") };
            yield return new object[] { new DicomAgeString(DicomTag.PatientAge, "012W") };

            yield return new object[] { new DicomCodeString(DicomTag.AcquisitionStartCondition, "0123456789 ") };
            yield return new object[] { new DicomDate(DicomTag.AcquisitionDate, "20210313") };
            yield return new object[] { new DicomDecimalString(DicomTag.ActiveSourceLength, "1e1") };

            yield return new object[] { new DicomFloatingPointSingle(DicomTag.AnchorPoint, ByteConverter.ToByteBuffer(new float[] { float.MaxValue })) };
            yield return new object[] { new DicomFloatingPointDouble(DicomTag.DopplerCorrectionAngle, ByteConverter.ToByteBuffer(new double[] { double.MaxValue })) };

            yield return new object[] { new DicomIntegerString(DicomTag.DoseReferenceNumber, "012345678912") };
            yield return new object[] { new DicomLongString(DicomTag.WindowCenterWidthExplanation, "0123456789012345678901234567890123456789012345678901234567891234") };
            yield return new object[] { new DicomPersonName(DicomTag.PatientName, "abc^xyz=abc^xyz^xyz^xyz^xyz=abc^xyz") };

            yield return new object[] { new DicomShortString(DicomTag.AccessionNumber, "0123456789123456") };
            yield return new object[] { new DicomSignedLong(DicomTag.DisplayedAreaBottomRightHandCorner, int.MaxValue) };
            yield return new object[] { new DicomSignedShort(DicomTag.LargestImagePixelValue, short.MaxValue) };

            yield return new object[] { new DicomUniqueIdentifier(DicomTag.DigitalSignatureUID, "13.14.520") };
            yield return new object[] { new DicomUnsignedLong(DicomTag.DopplerSampleVolumeXPositionRetiredRETIRED, uint.MaxValue) };
            yield return new object[] { new DicomUnsignedShort(DicomTag.AcquisitionMatrix, ushort.MaxValue) };
        }
    }
}
