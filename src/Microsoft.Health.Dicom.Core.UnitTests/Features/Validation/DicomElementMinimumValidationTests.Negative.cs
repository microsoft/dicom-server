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
    /// <summary>
    /// Test class for  DicomElementMinimumValidation
    /// </summary>
    public partial class DicomElementMinimumValidationTests
    {
        [Theory]
        [MemberData(nameof(AEInvalidValues))]
        public void GivenAEInvalidValue_WhenValidating_Throws(string value)
        {
            DicomElement element = new DicomApplicationEntity(DicomTag.DestinationAE, value);
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateAE(element));
        }

        [Theory]
        [InlineData("0.1D")] // decimal
        [InlineData("001w")] // lower case
        [InlineData("9999M")] // too long
        public void GivenASInvalidValue_WhenValidating_Throws(string value)
        {
            DicomElement element = new DicomAgeString(DicomTag.PatientAge, value);
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateAS(element));
        }

        [Fact]
        public void GivenATInvalidValue_WhenValidating_Throws()
        {
            DicomElement element = new DicomAttributeTag(DicomTag.DataElementsSigned, ByteConverter.ToByteBuffer<long>(new long[] { 100 })); // exceeds max length
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateAT(element));
        }

        [Theory]
        [InlineData("0123456789abcdefg")]
        public void GivenCSInvalidValue_WhenValidating_Throws(string value)
        {
            DicomElement element = new DicomCodeString(DicomTag.AcquisitionStartCondition, value);
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateCS(element));
        }

        [Theory]
        [InlineData("20100141")]
        [InlineData("233434343")]
        public void GivenDAInvalidValue_WhenValidating_Throws(string value)
        {
            DicomElement element = new DicomDate(DicomTag.AcquisitionDate, value);
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateDA(element));
        }

        [Theory]
        [InlineData("0123456789abcdefg")]
        public void GivenDSInvalidValue_WhenValidating_Throws(string value)
        {
            DicomElement element = new DicomDecimalString(DicomTag.ActiveSourceLength, value);
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateDS(element));
        }

        [Theory]
        [InlineData("20120")]
        public void GivenDTInvalidValue_WhenValidating_Throws(string value)
        {
            DicomElement element = new DicomDateTime(DicomTag.AcquisitionDateTime, value);
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateDT(element));
        }

        [Theory]
        [InlineData(100.123)]
        public void GivenFLInvalidValue_WhenValidating_Throws(float value)
        {
            DicomElement element = new DicomFloatingPointSingle(DicomTag.AnchorPoint, ByteConverter.ToByteBuffer<double>(new double[] { value }));
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateFL(element));
        }

        [Fact]
        public void GivenFDInvalidValue_WhenValidating_Throws()
        {
            // More than 8 bytes
            DicomElement element = new DicomFloatingPointDouble(DicomTag.DopplerCorrectionAngle, ByteConverter.ToByteBuffer<byte>(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateFD(element));
        }

        [Theory]
        [InlineData("0123456789abcdefg")]
        public void GivenISInvalidValue_WhenValidating_Throws(string value)
        {
            DicomElement element = new DicomIntegerString(DicomTag.DoseReferenceNumber, value);
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateIS(element));
        }

        [Theory]
        [InlineData("0123456789012345678901234567890123456789012345678901234567890123456789")]
        [InlineData("abc\\efg")]
        public void GivenLOInvalidValue_WhenValidating_Throws(string value)
        {
            DicomElement element = new DicomLongString(DicomTag.WindowCenterWidthExplanation, value);
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateLO(element));
        }

        [Theory]
        [InlineData("abc^xyz=abc^xyz=abc^xyz=abc^xyz")]
        [InlineData("abc^efg^hij^pqr^lmn^xyz")]
        [InlineData("0123456789012345678901234567890123456789012345678901234567890123456789")]
        public void GivenPNInvalidValue_WhenValidating_Throws(string value)
        {
            DicomElement element = new DicomPersonName(DicomTag.PatientName, value);
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidatePN(element));
        }

        [Theory]
        [InlineData("01234567891234567")] // length > 16
        public void GivenSHInvalidValue_WhenValidating_Throws(string value)
        {
            DicomElement element = new DicomShortString(DicomTag.AccessionNumber, value);
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateSH(element));
        }

        [Theory]
        [InlineData(100)]
        public void GivenSLInvalidValue_WhenValidating_Throws(int value)
        {
            DicomElement element = new DicomSignedLong(DicomTag.DisplayedAreaBottomRightHandCorner, ByteConverter.ToByteBuffer<long>(new long[] { value }));
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateSL(element));
        }

        [Theory]
        [InlineData(1000)]
        public void GivenSSInvalidValue_WhenValidating_Throws(short value)
        {
            DicomElement element = new DicomSignedShort(DicomTag.LargestImagePixelValue, ByteConverter.ToByteBuffer<int>(new int[] { value }));
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateSS(element));
        }

        [Theory]
        [InlineData("241200")] // hour > 23
        public void GivenTMInvalidValue_WhenValidating_Throws(string value)
        {
            DicomElement element = new DicomTime(DicomTag.AcquisitionTime, value);
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateTM(element));
        }

        [Theory]
        [InlineData("abc.123")]
        [InlineData("11|")]
        [InlineData("0123456789012345678901234567890123456789012345678901234567890123456789")]
        public void GivenUIInvalidValue_WhenValidating_Throws(string id)
        {
            Assert.Throws<InvalidIdentifierException>(() => DicomElementMinimumValidation.ValidateUI(id, nameof(id)));
        }

        [Fact]
        public void GivenULInvalidValue_WhenValidating_Throws()
        {
            DicomElement element = new DicomUnsignedLong(DicomTag.DopplerSampleVolumeXPositionRetiredRETIRED, ByteConverter.ToByteBuffer<byte>(new byte[] { 1, 2, 3, 4, 5 })); // exceed max length
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateUL(element));
        }

        [Theory]
        [InlineData(100)]
        public void GivenUSInvalidValue_WhenValidating_Throws(ushort value)
        {
            DicomElement element = new DicomUnsignedShort(DicomTag.AcquisitionMatrix, ByteConverter.ToByteBuffer<int>(new int[] { value }));
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateUS(element));
        }
    }
}
