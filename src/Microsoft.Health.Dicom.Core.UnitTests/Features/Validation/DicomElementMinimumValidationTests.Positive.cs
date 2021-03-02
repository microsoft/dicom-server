// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Dicom.IO;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation
{
    /// <summary>
    /// Test class for  DicomElementMinimumValidation
    /// </summary>
    public partial class DicomElementMinimumValidationTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("0123456789123456")] // max length
        public void GivenAEValidValue_WhenValidating_ThenShouldSucceed(string value)
        {
            DicomElement element = new DicomApplicationEntity(DicomTag.DestinationAE, value);
            DicomElementMinimumValidation.ValidateAE(element);
        }

        [Theory]
        [InlineData("011D")]
        [InlineData("123W")]
        [InlineData("999M")]
        [InlineData("777Y")]
        public void GivenASValidValue_WhenValidating_ThenShouldSucceed(string value)
        {
            DicomElement element = new DicomAgeString(DicomTag.PatientAge, value);
            DicomElementMinimumValidation.ValidateAE(element);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(uint.MaxValue)]
        [InlineData(uint.MinValue)]
        public void GivenATValidValue_WhenValidating_ThenShouldSucceed(uint value)
        {
            DicomElement element = new DicomAttributeTag(DicomTag.DataElementsSigned, ByteConverter.ToByteBuffer<uint>(new uint[] { value }));
            DicomElementMinimumValidation.ValidateAT(element);
        }

        [Theory]
        [InlineData("0123456789 _")] // all possible charactors
        [InlineData("")]
        [InlineData("0123456789123456")] // max length
        public void GivenCSValidValue_WhenValidating_ThenShouldSucceed(string value)
        {
            DicomElement element = new DicomCodeString(DicomTag.AcquisitionStartCondition, value);
            DicomElementMinimumValidation.ValidateCS(element);
        }

        [Theory]
        [InlineData("20210313")] // YYYYMMDD
        public void GivenDAValidValue_WhenValidating_ThenShouldSucceed(string value)
        {
            DicomElement element = new DicomDate(DicomTag.AcquisitionDate, value);
            DicomElementMinimumValidation.ValidateDA(element);
        }

        [Theory]
        [InlineData("+0123456789")]
        [InlineData("-123.123")] // minus and decimal
        [InlineData("1e1")] // exponential
        [InlineData("0123456789123456")] // max length
        public void GivenDSValidValue_WhenValidating_ThenShouldSucceed(string value)
        {
            DicomElement element = new DicomDecimalString(DicomTag.ActiveSourceLength, value);
            DicomElementMinimumValidation.ValidateDS(element);
        }

        // NOTES: there are several cases are not supported now:
        // 2021052013+0800(YYYYMMDDHH&ZZXX), 20210520-0800(YYYYMMDD&ZZXX), 202105-0800(YYYYMM&ZZXX), 2021+0800(YYYY&ZZXX)
        // We should consider creating our own parser
        [Theory]
        [InlineData("20210520131401.111111+0630")] // YYYYMMDDHHMMSS.FFFFFF&ZZXX
        [InlineData("20210520131401.111111")] // YYYYMMDDHHMMSS.FFFFFF
        [InlineData("20210520131401+0800")] // YYYYMMDDHHMMSS&ZZXX
        [InlineData("20210520131401")] // YYYYMMDDHHMMSS
        [InlineData("202105201314+0800")] // YYYYMMDDHHMM&ZZXX
        [InlineData("202105201314")] // YYYYMMDDHHMM
        [InlineData("2021052013-0800")] // YYYYMMDDHH&ZZXX
        [InlineData("2021052013")] // YYYYMMDDHH
        [InlineData("20210520-0800")] // YYYYMMDD&ZZXX
        [InlineData("20210520")] // YYYYMMDD
        [InlineData("202105-0800")] // YYYYMM&ZZXX
        [InlineData("202105")] // YYYYMM
        [InlineData("2021-0800")] // YYYY&ZZXX
        [InlineData("2021")] // YYYY
        public void GivenDTValidValue_WhenValidating_ThenShouldSucceed(string value)
        {
            DicomElement element = new DicomDateTime(DicomTag.AcquisitionDateTime, value);
            DicomElementMinimumValidation.ValidateDT(element);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(float.MinValue)]
        [InlineData(float.MaxValue)]
        public void GivenFLValidValue_WhenValidating_ThenShouldSucceed(float value)
        {
            DicomElement element = new DicomFloatingPointSingle(DicomTag.OphthalmicAxialLength, value);
            DicomElementMinimumValidation.ValidateFL(element);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(double.MinValue)]
        [InlineData(double.MaxValue)]
        public void GivenFDValidValue_WhenValidating_ThenShouldSucceed(double value)
        {
            DicomElement element = new DicomFloatingPointDouble(DicomTag.DopplerCorrectionAngle, ByteConverter.ToByteBuffer(new double[] { value }));
            DicomElementMinimumValidation.ValidateFD(element);
        }

        [Theory]
        [InlineData("")]
        [InlineData("+123")]
        [InlineData("-123")]
        [InlineData("012345678912")] // max length
        public void GivenISValidValue_WhenValidating_ThenShouldSucceed(string value)
        {
            DicomElement element = new DicomIntegerString(DicomTag.DoseReferenceNumber, value);
            DicomElementMinimumValidation.ValidateIS(element);
        }

        [Theory]
        [InlineData("")]
        [InlineData("0123456789012345678901234567890123456789012345678901234567891234")] // max length 64
        public void GivenLOValidValue_WhenValidating_ThenShouldSucceed(string value)
        {
            DicomElement element = new DicomLongString(DicomTag.WindowCenterWidthExplanation, value);
            DicomElementMinimumValidation.ValidateLO(element);
        }

        // TODO Next:
        [Theory]
        [InlineData("abc^xyz=abc^xyz^xyz^xyz^xyz=abc^xyz")]
        public void GivenPNValidValue_WhenValidating_ThenShouldSucceed(string value)
        {
            DicomElement element = new DicomPersonName(DicomTag.PatientName, value);
            DicomElementMinimumValidation.ValidatePN(element);
        }

        [Theory]
        [InlineData("")]
        [InlineData("0123456789123456")] // max length
        public void GivenSHValidValue_WhenValidating_ThenShouldSucceed(string value)
        {
            DicomElement element = new DicomShortString(DicomTag.AccessionNumber, value);
            DicomElementMinimumValidation.ValidateSH(element);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void GivenSLValidValue_WhenValidating_ThenShouldSucceed(int value)
        {
            DicomElement element = new DicomSignedLong(DicomTag.DisplayedAreaBottomRightHandCorner, value);
            DicomElementMinimumValidation.ValidateSL(element);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(short.MaxValue)]
        [InlineData(short.MinValue)]
        public void GivenSSValidValue_WhenValidating_ThenShouldSucceed(short value)
        {
            DicomElement element = new DicomSignedShort(DicomTag.LargestImagePixelValue, value);
            DicomElementMinimumValidation.ValidateSS(element);
        }

        [Theory]
        [InlineData("131405.000020")] // HHMMSS.FFFFFF
        [InlineData("131405")] // HHMMSS
        [InlineData("1314")] // HHMM
        [InlineData("13")] // HH
        public void GivenTMValidValue_WhenValidating_ThenShouldSucceed(string value)
        {
            DicomElement element = new DicomTime(DicomTag.AcquisitionTime, value);
            DicomElementMinimumValidation.ValidateTM(element);
        }

        [Theory]
        [InlineData("")]
        [InlineData("13.14.520")]
        public void GivenUIValidValue_WhenValidating_ThenShouldSucceed(string value)
        {
            DicomElement element = new DicomUniqueIdentifier(DicomTag.DigitalSignatureUID, value);
            DicomElementMinimumValidation.ValidateUI(element);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(uint.MinValue)]
        [InlineData(uint.MaxValue)]
        public void GivenULValidValue_WhenValidating_ThenShouldSucceed(uint value)
        {
            DicomElement element = new DicomUnsignedLong(DicomTag.DopplerSampleVolumeXPositionRetiredRETIRED, value);
            DicomElementMinimumValidation.ValidateUL(element);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(ushort.MaxValue)]
        [InlineData(ushort.MinValue)]
        public void GivenUSValidValue_WhenValidating_ThenShouldSucceed(ushort value)
        {
            DicomElement element = new DicomUnsignedShort(DicomTag.AcquisitionMatrix, value);
            DicomElementMinimumValidation.ValidateUS(element);
        }
    }
}
