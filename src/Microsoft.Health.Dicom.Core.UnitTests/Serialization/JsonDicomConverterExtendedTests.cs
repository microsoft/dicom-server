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

    [Theory]
    [InlineData("FL", "00720076", "Infinity")]
    [InlineData("FL", "00720076", "-Infinity")]
    [InlineData("FD", "00720074", "Infinity")]
    [InlineData("FD", "00720074", "-Infinity")]
    public static void GivenDicomJsonDatasetWithFloatingVRContainsInfinity_WhenDeserialized_IsSuccessful(string vr, string tag, string infinity)
    {
        string json = $@"
            {{
                ""{tag}"": {{
                    ""vr"": ""{vr}"",
                    ""Value"": [""{infinity}""]
                }}
            }}";

        DicomDataset dataset = JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions);
        Assert.NotNull(dataset);

        if (vr == "FL")
        {
            float value = dataset.GetSingleValue<float>(DicomTag.SelectorFLValue);
            Assert.True(infinity == "Infinity" ? float.IsPositiveInfinity(value) : float.IsNegativeInfinity(value));
        }
        else
        {
            double value = dataset.GetSingleValue<double>(DicomTag.SelectorFDValue);
            Assert.True(infinity == "Infinity" ? double.IsPositiveInfinity(value) : double.IsNegativeInfinity(value));
        }
    }

    [Fact]
    public static void GivenDicomJsonDatasetWithFlMixedInfinityAndNumber_WhenDeserialized_IsSuccessful()
    {
        // AHDS emits FL -Infinity as a JSON string because JSON has no numeric infinity.
        // A sibling finite number in the same Value array must still parse.
        const string json = @"
            {
                ""00720076"": {
                    ""vr"": ""FL"",
                    ""Value"": [""-Infinity"", -1]
                }
            }";

        DicomDataset dataset = JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions);
        float[] values = dataset.GetValues<float>(DicomTag.SelectorFLValue);

        Assert.Equal(2, values.Length);
        Assert.True(float.IsNegativeInfinity(values[0]));
        Assert.Equal(-1f, values[1]);
    }

    [Fact]
    public static void GivenDatasetWithFlAndFdInfinity_WhenSerialized_RoundTripsSpecialsAsStrings()
    {
        var dicomDataset = new DicomDataset
        {
            { DicomTag.SelectorFLValue, float.NegativeInfinity, -1f },
            { DicomTag.SelectorFDValue, double.PositiveInfinity, double.NaN },
        };

        string json = JsonSerializer.Serialize(dicomDataset, SerializerOptions);
        using JsonDocument document = JsonDocument.Parse(json);

        Assert.Equal("-Infinity", document.RootElement.GetProperty("00720076").GetProperty("Value")[0].GetString());
        Assert.Equal(-1, document.RootElement.GetProperty("00720076").GetProperty("Value")[1].GetDouble());
        Assert.Equal("Infinity", document.RootElement.GetProperty("00720074").GetProperty("Value")[0].GetString());
        Assert.Equal("NaN", document.RootElement.GetProperty("00720074").GetProperty("Value")[1].GetString());

        DicomDataset roundTripped = JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions);
        float[] fl = roundTripped.GetValues<float>(DicomTag.SelectorFLValue);
        double[] fd = roundTripped.GetValues<double>(DicomTag.SelectorFDValue);

        Assert.True(float.IsNegativeInfinity(fl[0]));
        Assert.Equal(-1f, fl[1]);
        Assert.True(double.IsPositiveInfinity(fd[0]));
        Assert.True(double.IsNaN(fd[1]));
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
