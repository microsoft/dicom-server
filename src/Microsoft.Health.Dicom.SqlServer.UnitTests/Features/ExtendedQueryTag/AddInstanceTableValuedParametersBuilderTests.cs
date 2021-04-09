// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.SqlServer.UnitTests.Features.Query
{
    public class AddInstanceTableValuedParametersBuilderTests
    {
        [Theory]
        [MemberData(nameof(GetSupportedDicomElement))]
        public void GivenSupportedDicomElement_WhenRead_ThenShouldReturnExpectedValue(DicomElement element, object expectedValue)
        {
            DicomDataset dataset = new DicomDataset();
            dataset.Add(element);
            QueryTag tag = new QueryTag(element.Tag.BuildExtendedQueryTagStoreEntry(vr: element.ValueRepresentation.Code));
            var parameters = AddInstanceTableValuedParametersBuilder.Build(dataset, new QueryTag[] { tag });

            ExtendedQueryTagDataType dataType = ExtendedQueryTagLimit.ExtendedQueryTagVRAndDataTypeMapping[element.ValueRepresentation.Code];
            switch (dataType)
            {
                case ExtendedQueryTagDataType.StringData:
                    Assert.Equal(expectedValue, parameters.StringExtendedQueryTags.First().TagValue);
                    break;
                case ExtendedQueryTagDataType.LongData:
                    Assert.Equal(expectedValue, parameters.LongExtendedQueryTags.First().TagValue);
                    break;
                case ExtendedQueryTagDataType.DoubleData:
                    Assert.Equal(expectedValue, parameters.DoubleExtendedQueryTags.First().TagValue);
                    break;
                case ExtendedQueryTagDataType.DateTimeData:
                    Assert.Equal(expectedValue, parameters.DateTimeExtendedQueryTags.First().TagValue);
                    break;
                case ExtendedQueryTagDataType.PersonNameData:
                    Assert.Equal(expectedValue, parameters.PersonNameExtendedQueryTags.First().TagValue);
                    break;
            }
        }

        public static IEnumerable<object[]> GetSupportedDicomElement()
        {
            yield return BuildParam(DicomTag.DestinationAE, "012", (tag, value) => new DicomApplicationEntity(tag, value));
            yield return BuildParam(DicomTag.PatientAge, "012W", (tag, value) => new DicomAgeString(tag, value));

            yield return BuildParam(DicomTag.AcquisitionStartCondition, "0123456789", (tag, value) => new DicomCodeString(tag, value));
            yield return BuildParam(DicomTag.AcquisitionDate, DateTime.Parse("2021/5/20"), (tag, value) => new DicomDate(tag, value));
            yield return BuildParam(DicomTag.ActiveSourceLength, "1e1", (tag, value) => new DicomDecimalString(tag, value));

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
