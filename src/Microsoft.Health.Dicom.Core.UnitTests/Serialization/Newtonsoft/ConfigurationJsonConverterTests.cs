// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Core.Serialization.Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Serialization.Newtonsoft;

public class ConfigurationJsonConverterTests
{
    private readonly JsonSerializerSettings _serializerSettings;

    public ConfigurationJsonConverterTests()
    {
        _serializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateParseHandling = DateParseHandling.None,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        };
        _serializerSettings.Converters.Add(new ConfigurationJsonConverter(new CamelCaseNamingStrategy()));
    }

    [Fact]
    public void GivenJson_WhenReadingWithNoDateParsing_ThenDeserialize()
    {
        _serializerSettings.DateParseHandling = DateParseHandling.None;

        const string json = @"{
  ""date"": ""2022-04-14T23:39:26.7818757Z"",
  ""dateOffset"": ""2022-05-03T15:35:57.2628853-03:00"",
}";

        IConfiguration actual = JsonConvert.DeserializeObject<IConfiguration>(json, _serializerSettings);

        Assert.Equal("2022-04-14T23:39:26.7818757Z", actual["date"]);
        Assert.Equal("2022-05-03T15:35:57.2628853-03:00", actual["dateOffset"]);
    }

    [Fact]
    public void GivenJson_WhenReadingWithDateTimeParsing_ThenDeserialize()
    {
        _serializerSettings.DateParseHandling = DateParseHandling.DateTime;

        const string json = @"{
  ""date"": ""2022-04-14T23:39:26.7818757Z"",
  ""dateOffset"": ""2022-05-03T15:35:57.2628853-03:00"",
}";

        IConfiguration actual = JsonConvert.DeserializeObject<IConfiguration>(json, _serializerSettings);

        Assert.Equal("2022-04-14T23:39:26.7818757Z", actual["date"]);
        Assert.Equal("2022-05-03T18:35:57.2628853Z", actual["dateOffset"]);
    }

    [Fact]
    public void GivenJson_WhenReadingWithDateTimeOffsetParsing_ThenDeserialize()
    {
        _serializerSettings.DateParseHandling = DateParseHandling.DateTimeOffset;

        const string json = @"{
  ""date"": ""2022-04-14T23:39:26.7818757Z"",
  ""dateOffset"": ""2022-05-03T15:35:57.2628853-03:00"",
}";

        IConfiguration actual = JsonConvert.DeserializeObject<IConfiguration>(json, _serializerSettings);

        Assert.Equal("2022-04-14T23:39:26.7818757+00:00", actual["date"]);
        Assert.Equal("2022-05-03T15:35:57.2628853-03:00", actual["dateOffset"]);
    }

    [Fact]
    public void GivenJson_WhenReadingWithDoubleParsing_ThenDeserialize()
    {
        _serializerSettings.FloatParseHandling = FloatParseHandling.Double;

        const string json = @"{
  ""float"": 12.345,
  ""double"": -6.78910,
  ""decimal"": 1112.1314151617181920
}";

        IConfiguration actual = JsonConvert.DeserializeObject<IConfiguration>(json, _serializerSettings);

        Assert.Equal("12.345", actual["float"]);
        Assert.Equal("-6.7891", actual["double"]);
        Assert.Equal("1112.1314151617182", actual["decimal"]); // lost precision
    }

    [Fact]
    public void GivenJson_WhenReadingWithDecimalParsing_ThenDeserialize()
    {
        _serializerSettings.FloatParseHandling = FloatParseHandling.Decimal;

        const string json = @"{
  ""float"": 12.345,
  ""double"": -6.78910,
  ""decimal"": 1112.1314151617181920
}";

        IConfiguration actual = JsonConvert.DeserializeObject<IConfiguration>(json, _serializerSettings);

        Assert.Equal("12.345", actual["float"]);
        Assert.Equal("-6.78910", actual["double"]);
        Assert.Equal("1112.1314151617181920", actual["decimal"]);
    }

    [Fact]
    public void GivenOverflowValue_WhenReadingWithDecimalParsing_ThenThrow()
    {
        _serializerSettings.FloatParseHandling = FloatParseHandling.Decimal;

        const string json = @"{
  ""decimal"": 1.7976931348623157E+308
}";

        Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<IConfiguration>(json, _serializerSettings));
    }

    [Fact]
    public void GivenJson_WhenReading_ThenDeserialize()
    {
        const string json = @"{
  ""string"": ""hello"",
  ""integer"": 42,
  ""guid"": ""138f3a3f-4de3-46b4-9c59-f4b33adfd50d"",
  ""time"": ""1.23:45:32.1"",
  ""uri"": ""http://example.com/unit/test?foo=bar"",
  ""array"": [
    ""zero"",
    ""one"",
    ""two""
  ],
  ""object"": {
    ""bool"": true,
    ""nested"": {
      ""float"": 1.2345,
      ""null"": null,
      ""empty"": {
      }
    }
  }
}";

        IConfiguration actual = JsonConvert.DeserializeObject<IConfiguration>(json, _serializerSettings);

        Assert.Equal("hello", actual["string"]);
        Assert.Equal("42", actual["integer"]);
        Assert.Equal("138f3a3f-4de3-46b4-9c59-f4b33adfd50d", actual["guid"]);
        Assert.Equal("1.23:45:32.1", actual["time"]);
        Assert.Equal("http://example.com/unit/test?foo=bar", actual["uri"]);
        Assert.Equal("zero", actual["array:0"]);
        Assert.Equal("one", actual["array:1"]);
        Assert.Equal("two", actual["array:2"]);
        Assert.Equal("True", actual["object:bool"]);
        Assert.Equal("1.2345", actual["object:nested:float"]);
        Assert.Null(actual["object:nested:null"]);
        Assert.Null(actual["object:nested:empty"]);
    }

    [Fact]
    public void GivenObject_WhenWriting_ThenSerialize()
    {
        const string expected = @"{
  ""array"": {
    ""0"": ""zero"",
    ""1"": ""one"",
    ""2"": ""two""
  },
  ""date"": ""2022-04-14T23:39:26.7818757Z"",
  ""dateOffset"": ""2022-05-03T15:35:57.2628853-07:00"",
  ""guid"": ""138f3a3f-4de3-46b4-9c59-f4b33adfd50d"",
  ""integer"": ""42"",
  ""object"": {
    ""bool"": ""true"",
    ""nested"": {
      ""float"": ""1.2345"",
      ""null"": {}
    }
  },
  ""string"": ""hello"",
  ""time"": ""1.23:45:32.1"",
  ""uri"": ""http://example.com/unit/test?foo=bar""
}";

        IConfiguration value = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new KeyValuePair<string, string>[]
                {
                    KeyValuePair.Create("string", "hello"),
                    KeyValuePair.Create("integer", "42"),
                    KeyValuePair.Create("date", "2022-04-14T23:39:26.7818757Z"),
                    KeyValuePair.Create("dateOffset", "2022-05-03T15:35:57.2628853-07:00"),
                    KeyValuePair.Create("guid", "138f3a3f-4de3-46b4-9c59-f4b33adfd50d"),
                    KeyValuePair.Create("time", "1.23:45:32.1"),
                    KeyValuePair.Create("uri", "http://example.com/unit/test?foo=bar"),
                    KeyValuePair.Create("array:0", "zero"),
                    KeyValuePair.Create("array:1", "one"),
                    KeyValuePair.Create("array:2", "two"),
                    KeyValuePair.Create("object:bool", "true"),
                    KeyValuePair.Create("object:nested:float", "1.2345"),
                    KeyValuePair.Create("object:nested:null", (string)null),
                })
            .Build();

        string actual = JsonConvert.SerializeObject(value, _serializerSettings);
        Assert.Equal(expected, actual);
    }
}
