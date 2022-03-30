// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using FellowOakDicom;
using Microsoft.Health.FellowOakDicom.Serialization;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Serialization;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Extensions;

public class JsonSerializerOptionsExtensionsTests
{
    private readonly JsonSerializerOptions _options;

    public JsonSerializerOptionsExtensionsTests()
    {
        _options = new JsonSerializerOptions();
        _options.ConfigureDefaultDicomSettings();
    }

    [Fact]
    public void GivenOptions_WhenConfiguringDefaults_ThenUpdateProperties()
    {
        Assert.Equal(3, _options.Converters.Count);
        Assert.Equal(typeof(StrictStringEnumConverterFactory), _options.Converters[0].GetType());
        Assert.Equal(typeof(DataSourceJsonConverterFactory), _options.Converters[1].GetType());
        Assert.Equal(typeof(DicomJsonConverter), _options.Converters[2].GetType());

        Assert.True(_options.AllowTrailingCommas);
        Assert.Equal(JsonIgnoreCondition.WhenWritingNull, _options.DefaultIgnoreCondition);
        Assert.Equal(JsonNamingPolicy.CamelCase, _options.DictionaryKeyPolicy);
        Assert.Null(_options.Encoder);
        Assert.False(_options.IgnoreReadOnlyFields);
        Assert.False(_options.IgnoreReadOnlyProperties);
        Assert.False(_options.IncludeFields);
        Assert.Equal(0, _options.MaxDepth);
        Assert.Equal(JsonNumberHandling.Strict, _options.NumberHandling);
        Assert.True(_options.PropertyNameCaseInsensitive);
        Assert.Equal(JsonNamingPolicy.CamelCase, _options.PropertyNamingPolicy);
        Assert.Equal(JsonCommentHandling.Skip, _options.ReadCommentHandling);
        Assert.False(_options.WriteIndented);
    }

    [Fact]
    public void GivenSerializationOptions_WhenDeserializingNumbers_ThenAcceptOnlyNumberTokens()
    {
        // PascalCase + trailing comma too!
        const string json = @"
{
  ""Word"": ""Hello"",
  ""Number"": 123,
}
";

        Example actual = JsonSerializer.Deserialize<Example>(json, _options);
        Assert.Equal("Hello", actual.Word);
        Assert.Equal(123, actual.Number);

        // Invalid
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Example>(@"
{
  ""Word"": ""World"",
  ""Number"": ""456"",
}
",
            _options));
    }

    [Theory]
    [InlineData("50", 50)]
    [InlineData("50.0", 50)]
    [InlineData("\"50\"", 50)]
    [InlineData("-1.23e4", -1.23e4)]
    [InlineData("\"-1.23e4\"", -1.23e4)]
    public void GivenSerializationOptions_WhenDeserializingDsVr_ThenAcceptEitherTokenKind(string weightJson, decimal expected)
    {
        string json = @$"
{{
  ""00101030"":{{
    ""vr"":""DS"",
    ""Value"":[
      {weightJson}
    ]
  }}
}}
";
        DicomDataset actual = JsonSerializer.Deserialize<DicomDataset>(json, _options);
        Assert.Equal(expected, actual.GetValue<decimal>(DicomTag.PatientWeight, 0));
    }

    [Fact]
    public void GivenSerializationOptions_WhenDeserializingDicomComments_ThenSkipThem()
    {
        const string json = @"
{
  // Study Date
  ""00080020"":{
    ""vr"":""DA"", // Date
    ""Value"":[
      ""20080701""
    ]
  },
  // Modality
  ""00080060"":{
    ""vr"":""CS"", // Code String
    ""Value"":[
      ""XRAY""
    ]
  },
}
";

        DicomDataset actual = JsonSerializer.Deserialize<DicomDataset>(json, _options);
        Assert.Equal(new DateTime(2008, 07, 01), actual.GetValue<DateTime>(DicomTag.StudyDate, 0));
        Assert.Equal("XRAY", actual.GetValue<string>(DicomTag.Modality, 0));
    }

    [Fact]
    public void GivenSerializationOptions_WhenDeserializingCustomComments_ThenSkipThem()
    {
        const string json = @"
{
  // Comment one
  ""Word"": ""Foo Bar"", // Comment two
  ""Number"": 789,
  // Comment three
}
";

        _options.Converters.Add(new CommentlessConverter());
        Example ex = JsonSerializer.Deserialize<Example>(json, _options);
        Assert.Equal("Foo Bar", ex.Word);
        Assert.Equal(789, ex.Number);
    }

    private sealed class Example
    {
        public string Word { get; set; }

        public int Number { get; set; }
    }

    private sealed class CommentlessConverter : JsonConverter<Example>
    {
        public override Example Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            StringComparison comparison = options.PropertyNameCaseInsensitive
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            var example = new Example();
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        // Get property name
                        string name = reader.GetString();
                        if (!reader.Read())
                        {
                            throw new JsonException();
                        }

                        // Assign property
                        if (string.Equals(name, nameof(Example.Word), comparison))
                        {
                            example.Word = reader.GetString();
                        }
                        else if (string.Equals(name, nameof(Example.Number), comparison))
                        {
                            example.Number = reader.GetInt32();
                        }
                        else
                        {
                            throw new JsonException();
                        }
                        break;
                    case JsonTokenType.EndObject:
                        return example;
                    default:
                        throw new JsonException();
                }
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, Example value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
