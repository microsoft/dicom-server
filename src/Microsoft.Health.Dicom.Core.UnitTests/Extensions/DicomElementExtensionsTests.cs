// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Extensions
{
    public class DicomElementExtensionsTests
    {
        [Theory]
        [MemberData(nameof(GetSupportedDicomElement))]
        public void GivenSupportedDicomElement_WhenGetSingleValue_ThenShouldReturnExpectedValue(DicomElement element, object expectedValue)
        {
            Assert.Equal(expectedValue, element.GetSingleValue());
        }

        [Fact]
        public void GivenMultiVMDicomElement_WhenGetSingleValue_ThenShouldReturnNull()
        {
            DicomElement element = new DicomFloatingPointSingle(DicomTag.AnchorPoint, 100.0f, 200.0f);
            Assert.Null(element.GetSingleValue());
        }

        public static IEnumerable<object[]> GetSupportedDicomElement()
        {
            yield return BuildParam(DicomTag.DestinationAE, "012", (tag, value) => new DicomApplicationEntity(tag, value));
            yield return BuildParam(DicomTag.PatientAge, "012W", (tag, value) => new DicomAgeString(tag, value));
            yield return BuildParam(DicomTag.DataElementsSigned, DicomTagToULong(DicomTag.PatientName), (tag, value) => new DicomAttributeTag(tag, ULongToDicomTag(value)));

            yield return BuildParam(DicomTag.AcquisitionStartCondition, "0123456789", (tag, value) => new DicomCodeString(tag, value));
            yield return BuildParam(DicomTag.AcquisitionDate, DateTime.Parse("2021/5/20"), (tag, value) => new DicomDate(tag, value));
            yield return BuildParam(DicomTag.ActiveSourceLength, "1e1", (tag, value) => new DicomDecimalString(tag, value));

            yield return BuildParam(DicomTag.AcquisitionDateTime, DateTime.Parse("2021/05/20 10:13"), (tag, value) => new DicomDateTime(tag, value));
            yield return BuildParam(DicomTag.TableOfParameterValues, 100.0f, (tag, value) => new DicomFloatingPointSingle(tag, value));
            yield return BuildParam(DicomTag.DopplerCorrectionAngle, 100.0, (tag, value) => new DicomFloatingPointDouble(tag, value));

            yield return BuildParam(DicomTag.DoseReferenceNumber, "012345678912", (tag, value) => new DicomIntegerString(tag, value));
            yield return BuildParam(DicomTag.WindowCenterWidthExplanation, "0123456789012345678901234567890123456789012345678901234567891234", (tag, value) => new DicomLongString(tag, value));
            yield return BuildParam(DicomTag.PatientName, "abc^xyz=abc^xyz^xyz^xyz^xyz=abc^xyz", (tag, value) => new DicomPersonName(tag, value));

            yield return BuildParam(DicomTag.AccessionNumber, "0123456789123456", (tag, value) => new DicomShortString(tag, value));
            yield return BuildParam(DicomTag.DisplayedAreaBottomRightHandCorner, int.MaxValue, (tag, value) => new DicomSignedLong(tag, value));
            yield return BuildParam(DicomTag.LargestImagePixelValue, short.MaxValue, (tag, value) => new DicomSignedShort(tag, value));

            yield return BuildParam(DicomTag.AcquisitionTime, DateTime.Parse("0001/1/1 1:30"), (tag, value) => new DicomTime(tag, value));
            yield return BuildParam(DicomTag.DigitalSignatureUID, "13.14.520", (tag, value) => new DicomUniqueIdentifier(tag, value));
            yield return BuildParam(DicomTag.DopplerSampleVolumeXPositionRetiredRETIRED, uint.MaxValue, (tag, value) => new DicomUnsignedLong(tag, value));
            yield return BuildParam(DicomTag.AcquisitionMatrix, ushort.MaxValue, (tag, value) => new DicomUnsignedShort(tag, value));
        }

        private static ulong DicomTagToULong(DicomTag tag)
        {
            return (ulong)((tag.Group * 65536) + tag.Element);
        }

        private static DicomTag ULongToDicomTag(ulong value)
        {
            return new DicomTag((ushort)(value / 65536), (ushort)(value % 65536));
        }

        private static object[] BuildParam<T>(DicomTag tag, T value, Func<DicomTag, T, DicomElement> creator)
        {
            return new object[] { creator.Invoke(tag, value), value };
        }
    }
}
