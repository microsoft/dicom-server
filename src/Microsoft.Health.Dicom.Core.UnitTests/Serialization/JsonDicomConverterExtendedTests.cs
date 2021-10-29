// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text.Json;
using FellowOakDicom;
using FellowOakDicom.Serialization;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Serialization
{
    public class JsonDicomConverterExtendedTests
    {
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions();

        static JsonDicomConverterExtendedTests()
        {
            SerializerOptions.Converters.Add(new DicomJsonConverter());
        }

        [Fact]
        public static void GivenDatasetWithEscapedCharacters_WhenSerialized_IsDeserializedCorrectly()
        {
            var unlimitedTextValue = "Multi\nLine\ttab\"quoted\"formfeed\f";

            var dicomDataset = new DicomDataset
            {
                { DicomTag.StrainAdditionalInformation, unlimitedTextValue },
            };

            var json = JsonSerializer.Serialize(dicomDataset, SerializerOptions);
            JsonDocument.Parse(json);
            DicomDataset deserializedDataset = JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions);
            var recoveredString = deserializedDataset.GetValue<string>(DicomTag.StrainAdditionalInformation, 0);
            Assert.Equal(unlimitedTextValue, recoveredString);
        }

        [Fact]
        public static void GivenDatasetWithUnicodeCharacters_WhenSerialized_IsDeserializedCorrectly()
        {
            var unlimitedTextValue = "⚽";

            var dicomDataset = new DicomDataset { { DicomTag.StrainAdditionalInformation, unlimitedTextValue }, };

            var json = JsonSerializer.Serialize(dicomDataset, SerializerOptions);
            JsonDocument.Parse(json);
            DicomDataset deserializedDataset = JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions);
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

            var json = JsonSerializer.Serialize(dicomDataset, SerializerOptions);
            JsonDocument.Parse(json);
            DicomDataset deserializedDataset = JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions);
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

            var json = JsonSerializer.Serialize(dicomDataset, SerializerOptions);
            JsonDocument.Parse(json);
            DicomDataset deserializedDataset = JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions);
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
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions));
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
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions));
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
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions));
        }

        [Fact]
        public static void GivenDicomJsonDatasetWithFloatingVRContainsNAN_WhenDeserialized_IsSuccessful()
        {
            const string json = @"
            {
                ""00720076"": {
                    ""vr"": ""FL"",
                     ""Value"": [""NaN""]
                 }
            } ";

            DicomDataset tagValue = JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions);
            Assert.NotNull(tagValue.GetDicomItem<DicomFloatingPointSingle>(DicomTag.SelectorFLValue));
        }
    }
}
