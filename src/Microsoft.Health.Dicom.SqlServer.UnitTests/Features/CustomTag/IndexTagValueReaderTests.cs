// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.SqlServer.Features.CustomTag;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.SqlServer.UnitTests.Features.Query
{
    public class IndexTagValueReaderTests
    {
        [Theory]
        [MemberData(nameof(GetSupportedDicomElement))]
        public void GivenSupportedDicomElement_WhenRead_ThenShouldReturnExpectedValue(DicomElement element, object expectedValue)
        {
            DicomDataset dataset = new DicomDataset();
            dataset.Add(element);
            IDictionary<IndexTag, string> stringValues;
            IDictionary<IndexTag, long> longValues;
            IDictionary<IndexTag, double> doubleValues;
            IDictionary<IndexTag, DateTime> datetimeValues;
            IDictionary<IndexTag, string> personNameValues;

            IndexTag tag = IndexTag.FromCustomTagStoreEntry(element.Tag.BuildCustomTagStoreEntry());
            IndexTagValueReader.Read(
                dataset,
                new IndexTag[] { tag },
                out stringValues,
                out longValues,
                out doubleValues,
                out datetimeValues,
                out personNameValues);

            CustomTagDataType dataType = CustomTagLimit.CustomTagVRAndDataTypeMapping[element.ValueRepresentation.Code];
            switch (dataType)
            {
                case CustomTagDataType.StringData:
                    Assert.Equal(expectedValue, stringValues[tag]);
                    break;
                case CustomTagDataType.LongData:
                    Assert.Equal(expectedValue, longValues[tag]);
                    break;
                case CustomTagDataType.DoubleData:
                    Assert.Equal(expectedValue, doubleValues[tag]);
                    break;
                case CustomTagDataType.DateTimeData:
                    Assert.Equal(expectedValue, datetimeValues[tag]);
                    break;
                case CustomTagDataType.PersonNameData:
                    Assert.Equal(expectedValue, personNameValues[tag]);
                    break;
            }
        }

        public static IEnumerable<object[]> GetSupportedDicomElement()
        {
            yield return BuildParam(DicomTag.DestinationAE, "012", (tag, value) => new DicomApplicationEntity(tag, value));
            yield return BuildParam(DicomTag.PatientAge, "012W", (tag, value) => new DicomAgeString(tag, value));
            yield return BuildParam(DicomTag.DataElementsSigned, DicomTagToLong(DicomTag.PatientName), (tag, value) => new DicomAttributeTag(tag, LongToDicomTag(value)));

            yield return BuildParam(DicomTag.AcquisitionStartCondition, "0123456789", (tag, value) => new DicomCodeString(tag, value));
            yield return BuildParam(DicomTag.AcquisitionDate, DateTime.Parse("2021/5/20"), (tag, value) => new DicomDate(tag, value));
            yield return BuildParam(DicomTag.ActiveSourceLength, "1e1", (tag, value) => new DicomDecimalString(tag, value));

            yield return BuildParam(DicomTag.AcquisitionDateTime, DateTime.Parse("2021/05/20 10:13"), (tag, value) => new DicomDateTime(tag, value));
            yield return BuildParam(DicomTag.TableOfParameterValues, 100.0, (tag, value) => new DicomFloatingPointSingle(tag, (float)value));
            yield return BuildParam(DicomTag.DopplerCorrectionAngle, 100.0, (tag, value) => new DicomFloatingPointDouble(tag, value));

            yield return BuildParam(DicomTag.DoseReferenceNumber, "0123456789", (tag, value) => new DicomIntegerString(tag, value));
            yield return BuildParam(DicomTag.WindowCenterWidthExplanation, "0123456789012345678901234567890123456789012345678901234567891234", (tag, value) => new DicomLongString(tag, value));
            yield return BuildParam(DicomTag.PatientName, "abc^xyz=abc^xyz^xyz^xyz^xyz=abc^xyz", (tag, value) => new DicomPersonName(tag, value));

            yield return BuildParam(DicomTag.AccessionNumber, "0123456789123456", (tag, value) => new DicomShortString(tag, value));
            yield return BuildParam(DicomTag.ReferencePixelX0, (long)int.MaxValue, (tag, value) => new DicomSignedLong(tag, (int)value));
            yield return BuildParam(DicomTag.LargestImagePixelValue, (long)short.MaxValue, (tag, value) => new DicomSignedShort(tag, (short)value));

            yield return BuildParam(DicomTag.DigitalSignatureUID, "13.14.520", (tag, value) => new DicomUniqueIdentifier(tag, value));
            yield return BuildParam(DicomTag.DopplerSampleVolumeXPositionRetiredRETIRED, (long)uint.MaxValue, (tag, value) => new DicomUnsignedLong(tag, (uint)value));
            yield return BuildParam(DicomTag.AngularViewVector, (long)ushort.MaxValue, (tag, value) => new DicomUnsignedShort(tag, (ushort)value));
        }

        private static long DicomTagToLong(DicomTag tag)
        {
            return (long)((ulong)((tag.Group * 65536) + tag.Element));
        }

        private static DicomTag LongToDicomTag(long value)
        {
            ulong uvalue = (ulong)value;
            return new DicomTag((ushort)(uvalue / 65536), (ushort)(uvalue % 65536));
        }

        private static object[] BuildParam<T>(DicomTag tag, T value, Func<DicomTag, T, DicomElement> creator)
        {
            return new object[] { creator.Invoke(tag, value), value };
        }
    }
}
