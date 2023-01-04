// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text.Json;
using FellowOakDicom;
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
    public static void GivenDropDataWhenInvalid_WhenAttrHasInvalidValue_ThenDataIsDropped()
    {
        JsonSerializerOptions dropDataSerializerOptions = new JsonSerializerOptions();
        dropDataSerializerOptions.Converters.Add(new DicomJsonConverter(
            dropDataWhenInvalid: true,
            writeTagsAsKeywords: false,
            autoValidate: false));

        // FL vr type needs to be able to parse into a number. FoDicom Json converter checks for this
        // 00081196 is WarningReason
        const string json = @"
            {
                ""00081196"": {
                    ""vr"": ""US"",
                    ""Value"": [
                        ""NotANumber""
                    ]
                }
            }";
        DicomDataset dataset = JsonSerializer.Deserialize<DicomDataset>(json, dropDataSerializerOptions);
        DicomDataException thrownException = Assert.Throws<DicomDataException>(() => dataset.GetString(DicomTag.WarningReason));
        Assert.Equal("Tag: (0008,1196) not found in dataset", thrownException.Message);
    }

    [Fact]
    public static void GivenDropDataWhenInvalid_WhenMixValidAndInvalidData_ThenValidDataIsRetained()
    {
        JsonSerializerOptions dropDataSerializerOptions = new JsonSerializerOptions();
        dropDataSerializerOptions.Converters.Add(new DicomJsonConverter(
            dropDataWhenInvalid: true,
            writeTagsAsKeywords: false,
            autoValidate: false));

        const string json = @"
            {
                ""00081196"": {
                    ""vr"": ""US"",
                    ""Value"": [
                        ""NotANumber""
                    ]
                },
                ""00080051"": {
                    ""vr"": ""SQ"",
                    ""Value"": [
                        {
                            ""00100020"":{
                                ""vr"": ""LO"",
                                ""Value"": [ ""Hospital A"" ]
                            }
                        },
                        {
                            ""00100020"":{
                                ""vr"": ""LO"",
                                ""Value"": [ ""Hospital B"" ]
                            }
                        }
                    ]
                },
                ""00080301"": {
                    ""vr"": ""US"",
                    ""Value"": [
                        333,
                        ""NotANumber""
                    ]
                },
                ""00080040"": {
                    ""vr"": ""US"",
                    ""Value"": [
                        444
                    ]
                }
            }";

        DicomDataset dataset = JsonSerializer.Deserialize<DicomDataset>(json, dropDataSerializerOptions);

        //invalid
        DicomDataException warningReasonException = Assert.Throws<DicomDataException>(() => dataset.GetString(DicomTag.WarningReason));
        Assert.Equal("Tag: (0008,1196) not found in dataset", warningReasonException.Message);

        //valid, we can handle grabbing a valid sequence after an invalid value occurs
        DicomSequence sq = dataset.GetSequence(DicomTag.IssuerOfAccessionNumberSequence);
        Assert.Equal("Hospital A", sq.Items[0].GetString(DicomTag.PatientID));
        Assert.Equal("Hospital B", sq.Items[1].GetString(DicomTag.PatientID));

        // invalid
        DicomDataException privateGroupReferenceException = Assert.Throws<DicomDataException>(() => dataset.GetString(DicomTag.PrivateGroupReference));
        Assert.Equal("Tag: (0008,0301) not found in dataset", privateGroupReferenceException.Message);

        // valid
        Assert.Equal("444", dataset.GetString(DicomTag.DataSetTypeRETIRED));
    }

    [Fact]
    public static void GivenDropDataWhenInvalid_WhenInvalidValueInSQ_WeCanStillParseValidValuesFromAttrsBeforeAndAfterAsWellAsValidValuesWithinSq()
    {
        JsonSerializerOptions dropDataSerializerOptions = new JsonSerializerOptions();
        dropDataSerializerOptions.Converters.Add(new DicomJsonConverter(
            dropDataWhenInvalid: true,
            writeTagsAsKeywords: false,
            autoValidate: false));

        const string json = @"
            {
                ""00081196"": {
                    ""vr"": ""US"",
                    ""Value"": [
                        111
                    ]
                },
                ""00080051"": {
                    ""vr"": ""SQ"",
                    ""Value"": [
                        {
                            ""00100020"":{
                                ""vr"": ""LO"",
                                ""Value"": [ ""Hospital A"" ]
                            }
                        },
                        {
                            ""00100020"":{
                                ""vr"": ""US"",
                                ""Value"": [
                                    ""NotANumber""
                                ]
                            }
                        }
                    ]
                },
                ""00080301"": {
                    ""vr"": ""US"",
                    ""Value"": [
                        222
                    ]
                }
            }";


        DicomDataset dataset = JsonSerializer.Deserialize<DicomDataset>(json, dropDataSerializerOptions);

        //valid
        Assert.Equal("111", dataset.GetString(DicomTag.WarningReason));

        //partially invalid, we can handle skipping children in a sequence
        DicomSequence sq = dataset.GetSequence(DicomTag.IssuerOfAccessionNumberSequence);
        Assert.Equal(1, sq.Items.Count); // only the first was valid, the second was dropped
        Assert.Equal("Hospital A", sq.Items[0].GetString(DicomTag.PatientID));

        // valid
        Assert.Equal("222", dataset.GetString(DicomTag.PrivateGroupReference));
    }

    [Fact]
    public static void GivenDropDataWhenInvalid_WhenAllValuesInSQInvalid_WeCanStillParseValidValuesFromAttrsBeforeAndAfter()
    {
        JsonSerializerOptions dropDataSerializerOptions = new JsonSerializerOptions();
        dropDataSerializerOptions.Converters.Add(new DicomJsonConverter(
            dropDataWhenInvalid: true,
            writeTagsAsKeywords: false,
            autoValidate: false));

        const string json = @"
            {
                ""00081196"": {
                    ""vr"": ""US"",
                    ""Value"": [
                        111
                    ]
                },
                ""00080051"": {
                    ""vr"": ""SQ"",
                    ""Value"": [
                        {
                            ""00100020"":{
                                ""vr"": ""US"",
                                ""Value"": [
                                    ""NotANumber""
                                ]
                            }
                        },
                        {
                            ""00100020"":{
                                ""vr"": ""US"",
                                ""Value"": [
                                    ""NotANumber""
                                ]
                            }
                        }
                    ]
                },
                ""00080301"": {
                    ""vr"": ""US"",
                    ""Value"": [
                        222
                    ]
                }
            }";


        DicomDataset dataset = JsonSerializer.Deserialize<DicomDataset>(json, dropDataSerializerOptions);

        //valid
        Assert.Equal("111", dataset.GetString(DicomTag.WarningReason));

        //no valid data in whole sequence, we can handle skipping the whole sequence
        DicomDataException issuerOfAccessionNumberSequenceException = Assert.Throws<DicomDataException>(() => dataset.GetString(DicomTag.IssuerOfAccessionNumberSequence));
        Assert.Equal("Tag: (0008,0051) not found in dataset", issuerOfAccessionNumberSequenceException.Message);

        // valid
        Assert.Equal("222", dataset.GetString(DicomTag.PrivateGroupReference));
    }


    [Fact]
    public static void GivenDropDataWhenInvalid_WhenInvalidValuesInSQAreNested_WeCanStillParseValidValuesFromAttrsBeforeAndAfter()
    {
        // test sequence of sequences
        JsonSerializerOptions dropDataSerializerOptions = new JsonSerializerOptions();
        dropDataSerializerOptions.Converters.Add(new DicomJsonConverter(
            dropDataWhenInvalid: true,
            writeTagsAsKeywords: false,
            autoValidate: false));

        const string json = @"
            {
                ""00081196"": {
                    ""vr"": ""US"",
                    ""Value"": [
                        111
                    ]
                },
                ""00080051"": {
                    ""vr"": ""SQ"",
                    ""Value"": [
                        {
                            ""00100020"":{
                                ""vr"": ""SQ"",
                                ""Value"": [
                                    {
                                        ""00100020"":{
                                            ""vr"": ""SQ"",
                                            ""Value"": [
                                                {
                                                    ""00100020"":{
                                                        ""vr"": ""SQ"",
                                                        ""Value"": [
                                                            ""NotANumber""
                                                        ]
                                                    }
                                                }
                                            ]
                                        }
                                    }
                                ]
                            }
                        },
                        {
                            ""00100020"":{
                                ""vr"": ""LO"",
                                ""Value"": [ ""Hospital A"" ]
                            }
                        }
                    ]
                },
                ""00080301"": {
                    ""vr"": ""US"",
                    ""Value"": [
                        222
                    ]
                }
            }";


        DicomDataset dataset = JsonSerializer.Deserialize<DicomDataset>(json, dropDataSerializerOptions);

        //valid
        Assert.Equal("111", dataset.GetString(DicomTag.WarningReason));

        //partially invalid, we can handle skipping children in a sequence
        DicomSequence sq = dataset.GetSequence(DicomTag.IssuerOfAccessionNumberSequence);
        Assert.Equal(1, sq.Items.Count); // only the first was valid, the second was dropped
        Assert.Equal("Hospital A", sq.Items[0].GetString(DicomTag.PatientID));

        // valid
        Assert.Equal("222", dataset.GetString(DicomTag.PrivateGroupReference));
    }

    [Fact]
    public static void GivenDropDataWhenInvalidAndAValueWithInvalidJson_WhenDeserialized_ThenJsonReaderExceptionIsThrown()
    {
        JsonSerializerOptions dropDataSerializerOptions = new JsonSerializerOptions();
        dropDataSerializerOptions.Converters.Add(new DicomJsonConverter(
            dropDataWhenInvalid: true,
            writeTagsAsKeywords: false,
            autoValidate: false));

        // This is not valid DICOM JSON format and the format that the serializer expects
        // \T is unexpected and invalid JSON here. The Utf8JsonReader used throws the exception deep
        // in its Read code, so we'd have to create our own Reader and override Read to not do that if we
        // wanted to skip invalid JSON on reads
        // TODO add custom reader that lets us skip data
        const string json = @"
            {
                ""00101010"": {
                    ""vr"": ""AS"",
                    ""Value"": [
                        ""Y49\T""
                    ]
                }

            }";
        JsonException thrownException = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions));
        Assert.Contains("'T' is an invalid escapable character within a JSON string. The string should be correctly escaped.", thrownException.Message);
    }

    [Fact]
    public static void GivenDropDataWhenInvalidAndJsonIsInvalid_WhenDeserialized_ThenJsonReaderExceptionIsThrown()
    {
        JsonSerializerOptions dropDataSerializerOptions = new JsonSerializerOptions();
        dropDataSerializerOptions.Converters.Add(new DicomJsonConverter(
            dropDataWhenInvalid: true,
            writeTagsAsKeywords: false,
            autoValidate: false));

        // This is not valid DICOM JSON format and the format that the serializer expects
        const string json = @"
            {
              ""00081030"": ""vr:LO, Value: Study1""
            }
            ";
        JsonException thrownException = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions));
        Assert.Contains("invalid: StartObject expected at position", thrownException.Message);
        Assert.Contains("instead found String", thrownException.Message);
    }

    [Fact]
    public static void GivenInvalidJSON_WhenDeserialized_JsonReaderExceptionIsThrown()
    {
        // \T is unexpected and invalid JSON here
        const string json = @"
            {
              ""00081030"": {
                ""vr"": ""LO"",
                ""Value"": [ ""Study1\T"" ]
              }
            }
            ";
        JsonException thrownException = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions));
        Assert.Contains("'T' is an invalid escapable character within a JSON string. The string should be correctly escaped.", thrownException.Message);
    }

    [Fact]
    public static void GivenInvalidDicomJSON_WhenDeserialized_JsonReaderExceptionIsThrown()
    {
        // This is not valid DICOM JSON format and the format that the serializer expects
        const string json = @"
            {
              ""00081030"": ""vr:LO, Value: Study1""
            }
            ";
        JsonException thrownException = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions));
        Assert.Contains("invalid: StartObject expected at position", thrownException.Message);
        Assert.Contains("instead found String", thrownException.Message);
    }

    [Fact]
    public static void GivenVRKeyNotInExpectedCase_WhenDeserialized_JsonReaderExceptionIsThrown()
    {
        //serializer looks for vr, but "vr" is case sensitive, so this needed to have "vr" instead of "VR" to pass
        const string json = @"
            {
              ""00081030"": {
                ""VR"": ""LO"",
                ""Value"": [ ""Study1"" ]
              }
            }
            ";
        JsonException thrownException = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions));
        Assert.Equal("Malformed DICOM json. vr value missing", thrownException.Message);
    }

    [Fact]
    public static void GivenMissingVR_WhenDeserialized_JsonReaderExceptionIsThrown()
    {
        const string json = @"
            {
              ""00081030"": {
                ""Value"": [ ""Study1"" ]
              }
            }
            ";
        JsonException thrownException = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions));
        Assert.Equal("Malformed DICOM json. vr value missing", thrownException.Message);
    }

    [Fact]
    public static void GivenMissingValue_WhenDeserialized_JsonReaderExceptionIsThrown()
    {
        const string json = @"
            {
              ""00081030"": {
                ""VR"": ""LO""
              }
            }
            ";
        JsonException thrownException = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions));
        Assert.Equal("Malformed DICOM json. vr value missing", thrownException.Message);
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
        NotSupportedException thrownException = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<DicomDataset>(json, SerializerOptions));
        Assert.Contains("Unsupported value representation", thrownException.Message);
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
}
