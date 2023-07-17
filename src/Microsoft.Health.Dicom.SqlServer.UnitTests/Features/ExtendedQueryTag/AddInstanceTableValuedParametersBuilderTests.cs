// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.SqlServer.UnitTests.Features.Query;

public class AddInstanceTableValuedParametersBuilderTests
{
    [Theory]
    [MemberData(nameof(GetSupportedDicomElement))]
    public void GivenSupportedDicomElement_WhenRead_ThenShouldReturnExpectedValue(DicomElement element, int schemaVersion, object expectedValue)
    {
        DicomDataset dataset = new DicomDataset();
        dataset.Add(element);
        QueryTag tag = new QueryTag(element.Tag.BuildExtendedQueryTagStoreEntry(vr: element.ValueRepresentation.Code));
        var parameters = ExtendedQueryTagDataRowsBuilder.Build(dataset, new QueryTag[] { tag }, (SchemaVersion)schemaVersion);

        ExtendedQueryTagDataType dataType = ExtendedQueryTagLimit.ExtendedQueryTagVRAndDataTypeMapping[element.ValueRepresentation.Code];
        switch (dataType)
        {
            case ExtendedQueryTagDataType.StringData:
                Assert.Equal(expectedValue, parameters.StringRows.First().TagValue);
                break;
            case ExtendedQueryTagDataType.LongData:
                Assert.Equal(expectedValue, parameters.LongRows.First().TagValue);
                break;
            case ExtendedQueryTagDataType.DoubleData:
                Assert.Equal(expectedValue, parameters.DoubleRows.First().TagValue);
                break;
            case ExtendedQueryTagDataType.DateTimeData:
                Assert.Equal(expectedValue, parameters.DateTimeWithUtcRows.First().TagValue);
                break;
            case ExtendedQueryTagDataType.PersonNameData:
                Assert.Equal(expectedValue, parameters.PersonNameRows.First().TagValue);
                break;
        }
    }

    [Theory]
    [MemberData(nameof(GetSupportedDicomSequenceElement))]
    public void GivenSupportedDicomSequenceElement_WhenRead_ThenShouldReturnExpectedValue(DicomItem item, string path, int key, int schemaVersion, object expectedValue)
    {
        DicomDataset dataset = new DicomDataset();
        dataset.Add(item);

        QueryTag tag = new QueryTag(Tests.Common.Extensions.DicomTagExtensions.BuildWorkitemQueryTagStoreEntry(path, key, item.ValueRepresentation.Code));

        var parameters = ExtendedQueryTagDataRowsBuilder.Build(dataset, new QueryTag[] { tag }, (SchemaVersion)schemaVersion);

        ExtendedQueryTagDataType dataType = ExtendedQueryTagLimit.ExtendedQueryTagVRAndDataTypeMapping[DicomVR.SH.Code];
        switch (dataType)
        {
            case ExtendedQueryTagDataType.StringData:
                Assert.Equal(expectedValue, parameters.StringRows.First().TagValue);
                Assert.Single(parameters.StringRows);
                break;
            case ExtendedQueryTagDataType.LongData:
                Assert.Equal(expectedValue, parameters.LongRows.First().TagValue);
                break;
            case ExtendedQueryTagDataType.DoubleData:
                Assert.Equal(expectedValue, parameters.DoubleRows.First().TagValue);
                break;
            case ExtendedQueryTagDataType.DateTimeData:
                Assert.Equal(expectedValue, parameters.DateTimeWithUtcRows.First().TagValue);
                break;
            case ExtendedQueryTagDataType.PersonNameData:
                Assert.Equal(expectedValue, parameters.PersonNameRows.First().TagValue);
                break;
        }
    }

    [Fact]
    public void GivenSupportedDicomSequenceElement_WhenRead_ThenShouldReturnMultipleExpectedValues()
    {
        DicomDataset dataset = new DicomDataset();
        var item = new DicomSequence(
                DicomTag.ReferencedRequestSequence,
                    new DicomDataset[] {
                        new DicomDataset(
                            new DicomShortString(
                            DicomTag.AccessionNumber, "Foo"),
                            new DicomShortString(
                            DicomTag.RequestedProcedureID, "Bar"))});
        dataset.Add(item);

        QueryTag tag1 = new QueryTag(Tests.Common.Extensions.DicomTagExtensions.BuildWorkitemQueryTagStoreEntry("0040A370.00080050", 1, item.ValueRepresentation.Code));
        QueryTag tag2 = new QueryTag(Tests.Common.Extensions.DicomTagExtensions.BuildWorkitemQueryTagStoreEntry("0040A370.00401001", 2, item.ValueRepresentation.Code));

        var parameters = ExtendedQueryTagDataRowsBuilder.Build(dataset, new QueryTag[] { tag1, tag2 }, (SchemaVersion)SchemaVersionConstants.Max);

        Assert.Equal(2, parameters.StringRows.Count());
    }

    public static IEnumerable<object[]> GetSupportedDicomElement()
    {
        for (int schemaVersion = SchemaVersionConstants.Min; schemaVersion <= SchemaVersionConstants.Max; schemaVersion++)
        {
            yield return BuildParam(DicomTag.DestinationAE, "012", schemaVersion, (tag, value) => new DicomApplicationEntity(tag, value));
            yield return BuildParam(DicomTag.PatientAge, "012W", schemaVersion, (tag, value) => new DicomAgeString(tag, value));

            yield return BuildParam(DicomTag.AcquisitionStartCondition, "0123456789", schemaVersion, (tag, value) => new DicomCodeString(tag, value));
            yield return BuildParam(DicomTag.AcquisitionDate, DateTime.Parse("2021/5/20", CultureInfo.InvariantCulture), schemaVersion, (tag, value) => new DicomDate(tag, value));

            yield return BuildParam(DicomTag.TableOfParameterValues, 100.0, schemaVersion, (tag, value) => new DicomFloatingPointSingle(tag, (float)value));
            yield return BuildParam(DicomTag.DopplerCorrectionAngle, 100.0, schemaVersion, (tag, value) => new DicomFloatingPointDouble(tag, value));

            yield return BuildParam(DicomTag.DoseReferenceNumber, "0123456789", schemaVersion, (tag, value) => new DicomIntegerString(tag, value), 123456789L);
            yield return BuildParam(DicomTag.WindowCenterWidthExplanation, "0123456789012345678901234567890123456789012345678901234567891234", schemaVersion, (tag, value) => new DicomLongString(tag, value));
            yield return BuildParam(DicomTag.PatientName, "abc^xyz=abc^xyz^xyz^xyz^xyz=abc^xyz", schemaVersion, (tag, value) => new DicomPersonName(tag, value));

            yield return BuildParam(DicomTag.AccessionNumber, "0123456789123456", schemaVersion, (tag, value) => new DicomShortString(tag, value));
            yield return BuildParam(DicomTag.ReferencePixelX0, (long)int.MaxValue, schemaVersion, (tag, value) => new DicomSignedLong(tag, (int)value));
            yield return BuildParam(DicomTag.LargestImagePixelValue, (long)short.MaxValue, schemaVersion, (tag, value) => new DicomSignedShort(tag, (short)value));

            yield return BuildParam(DicomTag.DigitalSignatureUID, "13.14.520", schemaVersion, (tag, value) => new DicomUniqueIdentifier(tag, value));
            yield return BuildParam(DicomTag.DopplerSampleVolumeXPositionRetiredRETIRED, (long)uint.MaxValue, schemaVersion, (tag, value) => new DicomUnsignedLong(tag, (uint)value));
            yield return BuildParam(DicomTag.AngularViewVector, (long)ushort.MaxValue, schemaVersion, (tag, value) => new DicomUnsignedShort(tag, (ushort)value));
        }
    }

    public static IEnumerable<object[]> GetSupportedDicomSequenceElement()
    {
        for (int schemaVersion = SchemaVersionConstants.Min; schemaVersion <= SchemaVersionConstants.Max; schemaVersion++)
        {
            yield return new object[] { new DicomSequence(
                DicomTag.ReferencedRequestSequence,
                    new DicomDataset[] {
                        new DicomDataset(
                            new DicomShortString(
                            DicomTag.AccessionNumber, "Foo"))}),
                "0040A370.00080050", 1, schemaVersion, "Foo" };

            yield return new object[] { new DicomSequence(
                DicomTag.ReferencedRequestSequence,
                    new DicomDataset[] {
                        new DicomDataset(
                            new DicomShortString(
                            DicomTag.AccessionNumber, "Foo"),
                            new DicomShortString(
                            DicomTag.PatientName, "Bar"))}),
                "0040A370.00080050", 1, schemaVersion, "Foo" };

            yield return new object[] { new DicomSequence(
                DicomTag.ReferencedRequestSequence,
                    new DicomDataset[] {
                        new DicomDataset(
                            new DicomShortString(
                            DicomTag.RequestedProcedureID, "ProceduredId1"))}),
                "0040A370.00401001", 2, schemaVersion, "ProceduredId1" };

            yield return new object[] { new DicomSequence(
                DicomTag.ScheduledStationNameCodeSequence,
                    new DicomDataset[] {
                        new DicomDataset(
                            new DicomShortString(
                            DicomTag.CodeValue, "StationName1"))}),
                "00404025.00080100", 3, schemaVersion, "StationName1" };

            yield return new object[] { new DicomAgeString(
                 DicomTag.PatientAge, "012W"),
                DicomTag.PatientAge.GetPath(), 4, schemaVersion, "012W" };
        }
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

    private static object[] BuildParam<T>(DicomTag tag, T value, int schemaVersion, Func<DicomTag, T, DicomElement> creator, object expected = null)
    {
        return new object[] { creator.Invoke(tag, value), schemaVersion, expected ?? value };
    }
}
