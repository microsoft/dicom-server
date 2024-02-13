// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.FellowOakDicom.Serialization;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Serialization;

public class JsonDicomConverterExtendedTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions();

    static JsonDicomConverterExtendedTests()
    {
        SerializerOptions.Converters.Add(new DicomJsonConverter(writeTagsAsKeywords: false, autoValidate: false));
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
    public static void GivenDicomJsonDatasetWithInvalidNumberVR_WhenDeserializedWithAutoValidateTrue_NumberExpectedJsonExceptionIsThrown()
    {
        const string json = @"
            {
              ""00081030"": {
                ""vr"": ""IS"",
                ""Value"": [ ""01:02:03"" ]
              }
            }
            ";

        var serializerOptions = new JsonSerializerOptions
        {
            Converters = { new DicomJsonConverter(writeTagsAsKeywords: false, autoValidate: true) }
        };

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DicomDataset>(json, serializerOptions));
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


    [Fact]
    public void DeserializeDSWithNonNumericValueAsStringDoesNotThrowException()
    {
        // in DICOM Standard PS3.18 F.2.3.1 now VRs DS, IS SV and UV may be either number or string
        var json = @"
            {
                ""00101030"": {
                    ""vr"":""DS"",
                    ""Value"":[84.5]
                },
                ""00101020"": {
                    ""vr"":""DS"",
                    ""Value"":[""asd""]
                }

            }";

        var serializerOptions = new JsonSerializerOptions
        {
            Converters =
            {
                new DicomJsonConverter(autoValidate: false, numberSerializationMode: NumberSerializationMode.PreferablyAsNumber)
            }
        };

        var dataset = JsonSerializer.Deserialize<DicomDataset>(json, serializerOptions);
        Assert.NotNull(dataset);
        Assert.Equal(84.5m, dataset.GetSingleValue<decimal>(DicomTag.PatientWeight));
        Assert.Equal(@"asd", dataset.GetString(DicomTag.PatientSize));
    }

    [Fact]
    public void DeserializeISWithNonNumericValueAsStringDoesNotThrowException()
    {
        // in DICOM Standard PS3.18 F.2.3.1 now VRs DS, IS SV and UV may be either number or string
        var json = @"
            {
                ""00201206"": {
                    ""vr"":""IS"",
                    ""Value"":[311]
                },
                ""00201209"": {
                    ""vr"":""IS"",
                    ""Value"":[""asd""]
                },
                ""00201204"": {
                    ""vr"":""IS"",
                    ""Value"":[]
                }
            }";
        var serializerOptions = new JsonSerializerOptions
        {
            Converters =
            {
                new DicomJsonConverter(autoValidate: false, numberSerializationMode: NumberSerializationMode.PreferablyAsNumber)
            }
        };

        var dataset = JsonSerializer.Deserialize<DicomDataset>(json, serializerOptions);

        Assert.NotNull(dataset);
        Assert.Equal(311, dataset.GetSingleValue<decimal>(DicomTag.NumberOfStudyRelatedSeries));
        Assert.Equal(@"asd", dataset.GetString(DicomTag.NumberOfSeriesRelatedInstances));
    }


    [Fact]
    public void DeserializeSVWithNonNumericValueAsStringDoesNotThrowException()
    {
        // in DICOM Standard PS3.18 F.2.3.1 now VRs DS, IS SV and UV may be either number or string
        var json = @"
            {
                ""00101030"": {
                    ""vr"":""SV"",
                    ""Value"":[84]
                },
                ""00101020"": {
                    ""vr"":""SV"",
                    ""Value"":[""asd""]
                }

            }";
        var serializerOptions = new JsonSerializerOptions
        {
            Converters =
            {
                new DicomJsonConverter(autoValidate: false, numberSerializationMode: NumberSerializationMode.PreferablyAsNumber)
            }
        };

        var dataset = JsonSerializer.Deserialize<DicomDataset>(json, serializerOptions);

        Assert.NotNull(dataset);
        Assert.Equal(84, dataset.GetSingleValue<long>(DicomTag.PatientWeight));
        Assert.Equal(@"asd", dataset.GetString(DicomTag.PatientSize));
    }


    [Fact]
    public void DeserializeUVWithNonNumericValueAsStringDoesNotThrowException()
    {
        // in DICOM Standard PS3.18 F.2.3.1 now VRs DS, IS SV and UV may be either number or string
        var json = @"
            {
                ""00101030"": {
                    ""vr"":""UV"",
                    ""Value"":[84]
                },
                ""00101020"": {
                    ""vr"":""UV"",
                    ""Value"":[""asd""]
                }

            }";
        var serializerOptions = new JsonSerializerOptions
        {
            Converters =
            {
                new DicomJsonConverter(autoValidate: false, numberSerializationMode: NumberSerializationMode.PreferablyAsNumber)
            }
        };

        var dataset = JsonSerializer.Deserialize<DicomDataset>(json, serializerOptions);

        Assert.NotNull(dataset);
        Assert.Equal(84ul, dataset.GetSingleValue<ulong>(DicomTag.PatientWeight));
        Assert.Equal(@"asd", dataset.GetString(DicomTag.PatientSize));
    }

    [Fact]
    public static void GivenDicomJsonDatasetWithInvalidPrivateCreatorDataElement_WhenDeserialized_IsSuccessful()
    {
        // allowing deserializer to handle bad data more gracefully
        const string json = @"
            {
                ""00090010"": {
                    ""vr"": ""US"",
                     ""Value"": [
                        1234,
                        3333
                    ]
                 },
                ""00091001"": {
                    ""vr"": ""CS"",
                    ""Value"": [
                        ""00""
                    ]
                }
            } ";

        // make sure below serialization does not throw
        DicomDataset ds = JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions);
        Assert.NotNull(ds);
    }

    [Theory]
    [InlineData("2147384638123")]
    [InlineData("73.8")]
    [InlineData("InvalidNumber")]
    public static void GivenDatasetWithInvalidOrOverflowNumberForValueRepresentationIS_WhenSerialized_IsDeserializedCorrectly(string overflowNumber)
    {
        var dicomDataset = new DicomDataset().NotValidated();
        dicomDataset.Add(new DicomIntegerString(DicomTag.Exposure, new MemoryByteBuffer(Encoding.ASCII.GetBytes(overflowNumber))));

        var serializerOptions = new JsonSerializerOptions
        {
            Converters =
            {
                new DicomJsonConverter(autoValidate: false, numberSerializationMode: NumberSerializationMode.PreferablyAsNumber)
            }
        };

        var json = JsonSerializer.Serialize(dicomDataset, serializerOptions);
        JsonDocument.Parse(json);
        DicomDataset deserializedDataset = JsonSerializer.Deserialize<DicomDataset>(json, serializerOptions);
        var recoveredString = deserializedDataset.GetValue<string>(DicomTag.Exposure, 0);
        Assert.Equal(overflowNumber, recoveredString);
    }
}
