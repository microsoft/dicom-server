// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text;
using Dicom;
using Dicom.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Serialization
{
    public class JsonDicomConverterExtendedTests
    {
        [Fact]
        public static void GivenDatasetWithEscapedCharacters_WhenSerialized_IsDeserializedCorrectly()
        {
            var unlimitedTextValue = "Multi\nLine\ttab\"quoted\"formfeed\f";

            var dicomDataset = new DicomDataset
            {
                { DicomTag.StrainAdditionalInformation, unlimitedTextValue },
            };

            var json = JsonConvert.SerializeObject(dicomDataset, new JsonDicomConverter());
            JObject.Parse(json);
            DicomDataset deserializedDataset = JsonConvert.DeserializeObject<DicomDataset>(json, new JsonDicomConverter());
            var recoveredString = deserializedDataset.GetValue<string>(DicomTag.StrainAdditionalInformation, 0);
            Assert.Equal(unlimitedTextValue, recoveredString);
        }

        [Fact]
        public static void GivenDatasetWithUnicodeCharacters_WhenSerialized_IsDeserializedCorrectly()
        {
            var unlimitedTextValue = "⚽";

            var dicomDataset = new DicomDataset { { DicomTag.StrainAdditionalInformation, Encoding.UTF8, unlimitedTextValue }, };

            var json = JsonConvert.SerializeObject(dicomDataset, new JsonDicomConverter());
            JObject.Parse(json);
            DicomDataset deserializedDataset = JsonConvert.DeserializeObject<DicomDataset>(json, new JsonDicomConverter());
            var recoveredString = deserializedDataset.GetValue<string>(DicomTag.StrainAdditionalInformation, 0);
            Assert.Equal(unlimitedTextValue, recoveredString);
        }

        [Fact]
        public static void GivenDicomDatasetWithBase64EncodedPixelData_WhenSerialized_IsDeserializedCorrectly()
        {
            var pixelData = Enumerable.Range(0, 1 << 8).Select(v => (byte)v).ToArray();
            var dicomDataset = new DicomDataset
            {
                { DicomTag.PixelData, pixelData },
            };

            var json = JsonConvert.SerializeObject(dicomDataset, new JsonDicomConverter());
            JObject.Parse(json);
            DicomDataset deserializedDataset = JsonConvert.DeserializeObject<DicomDataset>(json, new JsonDicomConverter());
            var recoveredPixelData = deserializedDataset.GetValues<byte>(DicomTag.PixelData);
            Assert.Equal(pixelData, recoveredPixelData);
        }

        [Fact]
        public static void GivenOWDicomDatasetWithBase64EncodedPixelData_WhenSerialized_IsDeserializedCorrectly()
        {
            var pixelData = Enumerable.Range(0, 1 << 16).Select(v => (ushort)v).ToArray();
            var dicomDataset = new DicomDataset
            {
                new DicomOtherWord(DicomTag.PixelData, pixelData),
            };

            var json = JsonConvert.SerializeObject(dicomDataset, new JsonDicomConverter());
            JObject.Parse(json);
            DicomDataset deserializedDataset = JsonConvert.DeserializeObject<DicomDataset>(json, new JsonDicomConverter());
            var recoveredPixelData = deserializedDataset.GetValues<ushort>(DicomTag.PixelData);
            Assert.Equal(pixelData, recoveredPixelData);
        }

        [Fact]
        public static void GivenInvalidDicomJsonDataset_WhenDeserialized_JsonReaderExceptionIsThrown()
        {
            const string json = @"
            {
              ""00081030"": {
                ""VR"": ""LO"",
                ""Value"": [ ""Study1"" ]
              }
            }
            ";
            Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<DicomDataset>(json, new JsonDicomConverter()));
        }

        [Fact]
        public static void GivenDicomJsonDatasetWithInvalidVR_WhenDeserialized_NotSupportedExceptionIsThrown()
        {
            const string json = @"
            {
                ""00081030"": {
                ""vr"": ""BADVR"",
                ""Value"": [ ""Study1"" ]
                }
            }
            ";
            Assert.Throws<NotSupportedException>(() => JsonConvert.DeserializeObject<DicomDataset>(json, new JsonDicomConverter()));
        }

        [Fact]
        public static void GivenDicomJsonDatasetWithInvalidNumberVR_WhenDeserialized_NotSupportedExceptionIsThrown()
        {
            const string json = @"
            {
              ""00081030"": {
                ""vr"": ""IS"",
                ""Value"": [ ""0"" ]
              }
            }
            ";
            Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<DicomDataset>(json, new JsonDicomConverter()));
        }

        [Fact]
        public static void GivenDicomJsonDatasetWithRepeatedTags_WhenDeserializedWithDuplicatePropertyNameHandling_JsonReaderExceptionIsThrown()
        {
            const string json = @"
            {
              ""00081030"": {
                ""vr"": ""LO"",
                ""Value"": [ ""Study1"" ]
              },
              ""00081030"": {
                ""vr"": ""LO"",
                ""Value"": [ ""Study2"" ]
              }
            }
            ";
            Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<DicomDataset>(
                json,
                new JsonDicomConverter(
                    writeTagsAsKeywords: false)));
        }
    }
}
