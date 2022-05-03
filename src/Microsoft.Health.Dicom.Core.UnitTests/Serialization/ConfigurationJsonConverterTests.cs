// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Core.Serialization;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Serialization;

public class ConfigurationJsonConverterTests
{
    private readonly JsonSerializerOptions _serializerOptions;

    public ConfigurationJsonConverterTests()
    {
        _serializerOptions = new JsonSerializerOptions { WriteIndented = true };
        _serializerOptions.Converters.Add(new ConfigurationJsonConverter());
    }

    [Fact]
    public void GivenJson_WhenReading_ThenDeserialize()
    {
        const string json = @"{
  ""string"": ""hello"",
  ""integer"": 42,
  ""date"": ""2022-04-14T23:39:26.7818757Z"",
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

        IConfiguration actual = JsonSerializer.Deserialize<IConfiguration>(json, _serializerOptions);

        Assert.Equal("hello", actual["string"]);
        Assert.Equal("42", actual["integer"]);
        Assert.Equal("2022-04-14T23:39:26.7818757Z", actual["date"]);
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

        string actual = JsonSerializer.Serialize(value, _serializerOptions);
        Assert.Equal(expected, actual);
    }
}
