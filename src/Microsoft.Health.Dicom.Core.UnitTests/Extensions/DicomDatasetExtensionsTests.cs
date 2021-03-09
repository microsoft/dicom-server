// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dicom;
using Dicom.Serialization;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Extensions
{
    public class DicomDatasetExtensionsTests
    {
        private readonly DicomDataset _dicomDataset = new DicomDataset();

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
                new DateTime(2020, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                _dicomDataset.GetStringDateAsDate(DicomTag.StudyDate));
        }

        [Fact]
        public void GivenAnInvalidDicomDateValue_WhenGetStringDateAsDateTimeIsCalled_ThenNullShouldBeReturned()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            DicomValidation.AutoValidation = false;
#pragma warning restore CS0618 // Type or member is obsolete

            _dicomDataset.Add(DicomTag.StudyDate, "2010");

#pragma warning disable CS0618 // Type or member is obsolete
            DicomValidation.AutoValidation = true;
#pragma warning restore CS0618 // Type or member is obsolete

            Assert.Null(_dicomDataset.GetStringDateAsDate(DicomTag.StudyDate));
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
            var jsonSerializer = new JsonSerializer();

            jsonSerializer.Converters.Add(new JsonDicomConverter());

            Assert.Equal(
                ConvertToJson(expectedDicomDataset, jsonSerializer),
                ConvertToJson(copiedDicomDataset, jsonSerializer));

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

            string ConvertToJson(DicomDataset dicomDataset, JsonSerializer jsonSerializer)
            {
                var result = new StringBuilder();

                using (StringWriter stringWriter = new StringWriter(result))
                using (JsonWriter jsonWriter = new JsonTextWriter(stringWriter))
                {
                    jsonSerializer.Serialize(jsonWriter, dicomDataset);
                }

                return result.ToString();
            }
        }

        [Fact]
        public void GivenADicomDatasetWithStandardTag_WhenGetDicomTagsIsCalled_ThenShouldReturnCorrectValue()
        {
            DicomTag dicomTag = DicomTag.DestinationAE;
            _dicomDataset.AddOrUpdate(dicomTag, "123");
            IndexTag indexTag = IndexTag.FromCustomTagStoreEntry(dicomTag.BuildCustomTagStoreEntry());
            var result = _dicomDataset.GetMatchingDicomTags(new IndexTag[] { indexTag });
            Assert.Single(result);
            Assert.Equal(dicomTag, result[indexTag]);
        }

        [Fact]
        public void GivenADicomDatasetWithPrivateTag_WhenGetDicomTagsIsCalled_ThenShouldReturnCorrectValue()
        {
            DicomTag dicomTag = new DicomTag(0x0405, 0x1001, "PrivateCreator");
            DicomElement element = new DicomCodeString(dicomTag, "123");
            _dicomDataset.Add(element);
            IndexTag indexTag = IndexTag.FromCustomTagStoreEntry(dicomTag.BuildCustomTagStoreEntry(vr: element.ValueRepresentation.Code));
            var result = _dicomDataset.GetMatchingDicomTags(new IndexTag[] { indexTag });
            Assert.Single(result);
            Assert.Equal(dicomTag, result[indexTag]);
        }

        [Fact]
        public void GivenADicomDataSetWithoutTheStandardTag_WhenGetDicomTagsIsCalled_ThenShouldNotReturn()
        {
            DicomTag dicomTag = DicomTag.DestinationAE;
            IndexTag indexTag = IndexTag.FromCustomTagStoreEntry(dicomTag.BuildCustomTagStoreEntry());
            var result = _dicomDataset.GetMatchingDicomTags(new IndexTag[] { indexTag });
            Assert.Empty(result);
        }

        [Fact]
        public void GivenADicomDataSetWithoutThePrivateTag_WhenGetDicomTagsIsCalled_ThenShouldNotReturn()
        {
            DicomTag dicomTag = new DicomTag(0x0405, 0x1001, "PrivateCreator");
            IndexTag indexTag = IndexTag.FromCustomTagStoreEntry(dicomTag.BuildCustomTagStoreEntry(vr: DicomVRCode.CS));
            var result = _dicomDataset.GetMatchingDicomTags(new IndexTag[] { indexTag });
            Assert.Empty(result);
        }

        [Fact]
        public void GivenADicomDataSetWithThePrivateTagButDifferentVR_WhenGetDicomTagsIsCalled_ThenShouldNotReturn()
        {
            DicomTag dicomTag = new DicomTag(0x0405, 0x1001, "PrivateCreator");
            DicomElement element = new DicomIntegerString(dicomTag, "123");
            _dicomDataset.Add(element);
            IndexTag indexTag = IndexTag.FromCustomTagStoreEntry(dicomTag.BuildCustomTagStoreEntry(vr: DicomVRCode.CS));
            var result = _dicomDataset.GetMatchingDicomTags(new IndexTag[] { indexTag });
            Assert.Empty(result);
        }
    }
}
