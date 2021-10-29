// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FellowOakDicom;
using FellowOakDicom.Serialization;
using Microsoft.Health.Dicom.Core.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Extensions
{
    public class DicomDatasetExtensionsTests
    {
        private readonly DicomDataset _dicomDataset = new DicomDataset().NotValidated();

        [Fact]
        public void GivenDicomTagWithDifferentVR_WhenGetSingleOrDefaultIsCalled_ThenShouldReturnNull()
        {
            DicomTag tag = DicomTag.AbortReason;
            DicomVR expectedVR = DicomVR.CS;
            DicomElement element = new DicomLongString(tag, "Value");
            _dicomDataset.Add(element);
            Assert.Null(_dicomDataset.GetSingleValueOrDefault<string>(tag, expectedVR));
        }

        [Fact]
        public void GivenDicomTagDoesNotExist_WhenGetSingleOrDefaultIsCalled_ThenDefaultValueShouldBeReturned()
        {
            Assert.Equal(default, _dicomDataset.GetSingleValueOrDefault<string>(DicomTag.StudyInstanceUID));
            Assert.Equal(default, _dicomDataset.GetSingleValueOrDefault<DateTime>(DicomTag.AcquisitionDateTime));
            Assert.Equal(default, _dicomDataset.GetSingleValueOrDefault<short>(DicomTag.WarningReason));
        }

        [Fact]
        public void GivenDicomTagExists_WhenGetSingleOrDefaultIsCalled_ThenCorrectValueShouldBeReturned()
        {
            const string expectedValue = "IA";

            _dicomDataset.Add(DicomTag.InstanceAvailability, expectedValue);

            Assert.Equal(expectedValue, _dicomDataset.GetSingleValueOrDefault<string>(DicomTag.InstanceAvailability));
        }

        [Fact]
        public void GivenNoDicomDateValue_WhenGetStringDateAsDateTimeIsCalled_ThenNullShouldBeReturned()
        {
            Assert.Null(_dicomDataset.GetStringDateAsDate(DicomTag.StudyDate));
        }

        [Fact]
        public void GivenAValidDicomDateValue_WhenGetStringDateAsDateTimeIsCalled_ThenCorrectDateTimeShouldBeReturned()
        {
            _dicomDataset.Add(DicomTag.StudyDate, "20200301");

            Assert.Equal(
                new DateTime(2020, 3, 1, 0, 0, 0, 0, DateTimeKind.Local),
                _dicomDataset.GetStringDateAsDate(DicomTag.StudyDate).Value);
        }

        [Fact]
        public void GivenAnInvalidDicomDateValue_WhenGetStringDateAsDateTimeIsCalled_ThenNullShouldBeReturned()
        {
            _dicomDataset.Add(DicomTag.StudyDate, "2010");

            Assert.Null(_dicomDataset.GetStringDateAsDate(DicomTag.StudyDate));
        }

        [Fact]
        public void GivenNoDicomDateTimeValue_WhenGetStringDateTimeAsLiteralAndUtcDateTimesIsCalled_ThenNullShouldBeReturned()
        {
            Tuple<DateTime?, DateTime?> result = _dicomDataset.GetStringDateTimeAsLiteralAndUtcDateTimes(DicomTag.AcquisitionDateTime);
            Assert.Null(result.Item1);
            Assert.Null(result.Item2);
        }

        [Theory]
        [ClassData(typeof(DateTimeValidTestData))]
        public void GivenAValidDicomDateTimeValue_WhenGetStringDateTimeAsLiteralAndUtcDateTimesIsCalled_ThenCorrectLiteralDateTimesShouldBeReturned(
            string acquisitionDateTime,
            int year,
            int month,
            int day,
            int hour,
            int minute,
            int second,
            int millisecond
            )
        {
            _dicomDataset.Add(DicomTag.AcquisitionDateTime, acquisitionDateTime);
            Assert.Equal(
                new DateTime(
                    year,
                    month,
                    day,
                    hour,
                    minute,
                    second,
                    millisecond),
                _dicomDataset.GetStringDateTimeAsLiteralAndUtcDateTimes(DicomTag.AcquisitionDateTime).Item1.Value);
        }

        [Theory]
        [ClassData(typeof(DateTimeValidUtcTestData))]
        public void GivenAValidDicomDateTimeValueWithOffset_WhenGetStringDateTimeAsLiteralAndUtcDateTimesIsCalled_ThenCorrectUtcDateTimesShouldBeReturned(
            string acquisitionDateTime,
            int year,
            int month,
            int day,
            int hour,
            int minute,
            int second,
            int millisecond
            )
        {
            _dicomDataset.Add(DicomTag.AcquisitionDateTime, acquisitionDateTime);
            Assert.Equal(
                new DateTime(
                    year,
                    month,
                    day,
                    hour,
                    minute,
                    second,
                    millisecond),
                _dicomDataset.GetStringDateTimeAsLiteralAndUtcDateTimes(DicomTag.AcquisitionDateTime).Item2.Value);
        }

        [Theory]
        [ClassData(typeof(DateTimeWithTimezoneOffsetFromUtcValidTestData))]
        public void GivenAValidDicomDateTimeWithoutOffsetWithTimezoneOffsetFromUtc_WhenGetStringDateTimeAsLiteralAndUtcDateTimesIsCalled_ThenCorrectUtcDateTimesShouldBeReturned(
           string acquisitionDateTime,
           string timezoneOffsetFromUTC,
           int year,
           int month,
           int day,
           int hour,
           int minute,
           int second,
           int millisecond)
        {
            _dicomDataset.Add(DicomTag.AcquisitionDateTime, acquisitionDateTime);
            _dicomDataset.Add(DicomTag.TimezoneOffsetFromUTC, timezoneOffsetFromUTC);
            Assert.Equal(
                new DateTime(
                    year,
                    month,
                    day,
                    hour,
                    minute,
                    second,
                    millisecond),
                _dicomDataset.GetStringDateTimeAsLiteralAndUtcDateTimes(DicomTag.AcquisitionDateTime).Item2.Value);
        }

        [Fact]
        public void GivenAValidDicomDateTimeValueWithoutOffset_WhenGetStringDateTimeAsLiteralAndUtcDateTimesIsCalled_ThenNullIsReturnedForUtcDateTime()
        {
            _dicomDataset.Add(DicomTag.AcquisitionDateTime, "20200102030405.678");

            Assert.Null(_dicomDataset.GetStringDateTimeAsLiteralAndUtcDateTimes(DicomTag.AcquisitionDateTime).Item2);
        }

        [Theory]
        [InlineData("20200301010203.123+9900")]
        [InlineData("20200301010203.123-9900")]
        [InlineData("20200301010203123+0500")]
        [InlineData("20209901010203+0500")]
        [InlineData("20200399010203+0500")]
        [InlineData("20200301990203+0500")]
        [InlineData("20200301019903+0500")]
        [InlineData("20200301010299+0500")]
        [InlineData("20200301010299.")]
        [InlineData("20200301010299123")]
        [InlineData("20209901010203")]
        [InlineData("20200399010203")]
        [InlineData("20200301990203")]
        [InlineData("20200301019903")]
        [InlineData("20200301010299")]
        [InlineData("31")]
        public void GivenAnInvalidDicomDateTimeValue_WhenGetStringDateTimeAsLiteralAndUtcDateTimesIsCalled_ThenNullShouldBeReturned(string acquisitionDateTime)
        {
            _dicomDataset.Add(DicomTag.AcquisitionDateTime, acquisitionDateTime);

            Assert.Null(_dicomDataset.GetStringDateTimeAsLiteralAndUtcDateTimes(DicomTag.AcquisitionDateTime).Item1);
        }

        [Fact]
        public void GivenNoDicomTimeValue_WhenGetStringTimeAsLongIsCalled_ThenNullShouldBeReturned()
        {
            Assert.Null(_dicomDataset.GetStringTimeAsLong(DicomTag.StudyTime));
        }

        [Theory]
        [InlineData("010203.123", 01, 02, 03, 123)]
        [InlineData("010203", 01, 02, 03, 0)]
        [InlineData("0102", 01, 02, 0, 0)]
        [InlineData("01", 01, 0, 0, 0)]
        public void GivenAValidDicomTimeValue_WhenGetStringTimeAsLongIsCalled_ThenCorrectTimeTicksShouldBeReturned(
            string studyTime,
            int hour,
            int minute,
            int second,
            int millisecond
            )
        {
            _dicomDataset.Add(DicomTag.StudyTime, studyTime);
            Assert.Equal(
                new DateTime(
                    01,
                    01,
                    01,
                    hour,
                    minute,
                    second,
                    millisecond).Ticks,
                _dicomDataset.GetStringTimeAsLong(DicomTag.StudyTime).Value);
        }

        [Theory]
        [InlineData("010299123")]
        [InlineData("010299")]
        [InlineData("019903")]
        [InlineData("990203")]
        [InlineData("2")]
        public void GivenAnInvalidDicomTimeValue_WhenGetStringTimeAsLongIsCalled_ThenNullShouldBeReturned(string studyTime)
        {
            _dicomDataset.Add(DicomTag.StudyTime, studyTime);

            Assert.Null(_dicomDataset.GetStringTimeAsLong(DicomTag.StudyTime));
        }

        [Fact]
        public void GivenANullValue_WhenAddValueIfNotNullIsCalled_ThenValueShouldNotBeAdded()
        {
            DicomTag dicomTag = DicomTag.StudyInstanceUID;

            _dicomDataset.AddValueIfNotNull(dicomTag, (string)null);

            Assert.False(_dicomDataset.TryGetSingleValue(dicomTag, out string _));
        }

        [Fact]
        public void GivenANonNullValue_WhenAddValueIfNotNullIsCalled_ThenValueShouldBeAdded()
        {
            const string value = "123";

            DicomTag dicomTag = DicomTag.StudyInstanceUID;

            _dicomDataset.AddValueIfNotNull(dicomTag, value);

            Assert.True(_dicomDataset.TryGetSingleValue<string>(dicomTag, out string writtenValue));
            Assert.Equal(writtenValue, value);
        }

        [Fact]
        public void GivenADicomDataset_WhenCopiedWithoutBulkDataItems_ThenCorrectDicomDatasetShouldBeCreated()
        {
            var dicomItemsToCopy = new Dictionary<Type, DicomItem>();

            AddCopy(() => new DicomApplicationEntity(DicomTag.RetrieveAETitle, "ae"));
            AddCopy(() => new DicomAgeString(DicomTag.PatientAge, "011Y"));
            AddCopy(() => new DicomAttributeTag(DicomTag.DimensionIndexPointer, DicomTag.DimensionIndexSequence));
            AddCopy(() => new DicomCodeString(DicomTag.Modality, "MRI"));
            AddCopy(() => new DicomDate(DicomTag.StudyDate, "20200105"));
            AddCopy(() => new DicomDecimalString(DicomTag.PatientSize, "153.5"));
            AddCopy(() => new DicomDateTime(DicomTag.AcquisitionDateTime, new DateTime(2020, 5, 1, 13, 20, 49, 403, DateTimeKind.Utc)));
            AddCopy(() => new DicomFloatingPointSingle(DicomTag.RecommendedDisplayFrameRateInFloat, 1.5f));
            AddCopy(() => new DicomFloatingPointDouble(DicomTag.EventTimeOffset, 1.50));
            AddCopy(() => new DicomIntegerString(DicomTag.StageNumber, 1));
            AddCopy(() => new DicomLongString(DicomTag.EventTimerNames, "string1", "string2"));
            AddCopy(() => new DicomLongText(DicomTag.PatientComments, "comment"));
            AddCopy(() => new DicomPersonName(DicomTag.PatientName, "Jon^Doe"));
            AddCopy(() => new DicomShortString(DicomTag.Occupation, "IT"));
            AddCopy(() => new DicomSignedLong(DicomTag.ReferencePixelX0, -123));
            AddCopy(() => new DicomSignedShort(DicomTag.TagAngleSecondAxis, -50));
            AddCopy(() => new DicomShortText(DicomTag.CodingSchemeName, "text"));
            AddCopy(() => new DicomSignedVeryLong(new DicomTag(7777, 1234), -12345));
            AddCopy(() => new DicomTime(DicomTag.Time, "172230"));
            AddCopy(() => new DicomUnlimitedCharacters(DicomTag.LongCodeValue, "long value"));
            AddCopy(() => new DicomUniqueIdentifier(DicomTag.StudyInstanceUID, "1.2.3"));
            AddCopy(() => new DicomUnsignedLong(DicomTag.SimpleFrameList, 1, 2, 3));
            AddCopy(() => new DicomUniversalResource(DicomTag.RetrieveURL, "https://localhost/"));
            AddCopy(() => new DicomUnsignedShort(DicomTag.NumberOfElements, 50));
            AddCopy(() => new DicomUnlimitedText(DicomTag.StrainAdditionalInformation, "unlimited text"));
            AddCopy(() => new DicomUnsignedVeryLong(new DicomTag(7777, 9865), 243));

            var dicomItemsNotToCopy = new Dictionary<Type, DicomItem>();

            AddDoNotCopy(() => new DicomOtherByte(DicomTag.BadPixelImage, new byte[] { 1, 2, 3 }));
            AddDoNotCopy(() => new DicomOtherDouble(DicomTag.VolumetricCurvePoints, 12.3));
            AddDoNotCopy(() => new DicomOtherFloat(DicomTag.TwoDimensionalToThreeDimensionalMapData, 1.24f));
            AddDoNotCopy(() => new DicomOtherLong(DicomTag.LongPrimitivePointIndexList, 1));
            AddDoNotCopy(() => new DicomOtherVeryLong(DicomTag.ExtendedOffsetTable, 23242394));
            AddDoNotCopy(() => new DicomOtherWord(DicomTag.SegmentedAlphaPaletteColorLookupTableData, 213));
            AddDoNotCopy(() => new DicomUnknown(new DicomTag(7777, 1357), new byte[] { 10, 15, 20 }));

            var sequence = new DicomSequence(
                DicomTag.ReferencedSOPSequence,
                new DicomDataset(
                    dicomItemsNotToCopy[typeof(DicomOtherByte)],
                    dicomItemsNotToCopy[typeof(DicomOtherDouble)],
                    dicomItemsToCopy[typeof(DicomIntegerString)],
                    dicomItemsNotToCopy[typeof(DicomOtherFloat)]),
                new DicomDataset(
                    dicomItemsNotToCopy[typeof(DicomOtherLong)],
                    dicomItemsToCopy[typeof(DicomPersonName)],
                    dicomItemsNotToCopy[typeof(DicomOtherVeryLong)],
                    dicomItemsNotToCopy[typeof(DicomOtherWord)],
                    dicomItemsToCopy[typeof(DicomShortString)],
                    dicomItemsNotToCopy[typeof(DicomUnknown)],
                    new DicomSequence(
                        DicomTag.FailedSOPSequence,
                        new DicomDataset(
                            dicomItemsNotToCopy[typeof(DicomUnknown)]))));

            // Create a dataset that includes all VR types.
            DicomDataset dicomDataset = new DicomDataset(
                dicomItemsToCopy.Values.Concat(dicomItemsNotToCopy.Values).Concat(new[] { sequence }));

            // Make a copy of the dataset without the bulk data.
            DicomDataset copiedDicomDataset = dicomDataset.CopyWithoutBulkDataItems();

            Assert.NotNull(copiedDicomDataset);

            // Make sure it's a copy.
            Assert.NotSame(dicomDataset, copiedDicomDataset);

            // Make sure the original dataset was not altered.
            Assert.Equal(dicomItemsToCopy.Count + dicomItemsNotToCopy.Count + 1, dicomDataset.Count());

            // The expected number of items are dicomItemsToCopy + sequence.
            Assert.Equal(dicomItemsToCopy.Count + 1, copiedDicomDataset.Count());

            var expectedSequence = new DicomSequence(
                DicomTag.ReferencedSOPSequence,
                new DicomDataset(
                    dicomItemsToCopy[typeof(DicomIntegerString)]),
                new DicomDataset(
                    dicomItemsToCopy[typeof(DicomPersonName)],
                    dicomItemsToCopy[typeof(DicomShortString)],
                    new DicomSequence(
                        DicomTag.FailedSOPSequence,
                        new DicomDataset())));

            var expectedDicomDataset = new DicomDataset(
                dicomItemsToCopy.Values.Concat(new[] { expectedSequence }));

            // There is no easy way to compare the DicomItem (it doesn't implement IComparable or
            // have a consistent way of getting the value out of it. So we will cheat a little bit
            // by serialize the DicomDataset into JSON string. The serializer ensures the items are
            // ordered properly.
            var options = new JsonSerializerOptions();

            options.Converters.Add(new DicomJsonConverter());

            Assert.Equal(
                JsonSerializer.Serialize(expectedDicomDataset, options),
                JsonSerializer.Serialize(copiedDicomDataset, options));

            void AddCopy<T>(Func<T> creator)
                where T : DicomItem
            {
                dicomItemsToCopy.Add(typeof(T), creator());
            }

            void AddDoNotCopy<T>(Func<T> creator)
                where T : DicomItem
            {
                dicomItemsNotToCopy.Add(typeof(T), creator());
            }
        }
    }
}
