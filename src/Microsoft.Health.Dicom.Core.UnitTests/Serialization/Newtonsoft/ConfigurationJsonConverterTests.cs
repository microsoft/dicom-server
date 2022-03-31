// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Core.Serialization.Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
        };

        _serializerSettings.Converters.Add(new StringEnumConverter());
        _serializerSettings.Converters.Add(new ConfigurationJsonConverter());
    }

    [Fact]
    public void GivenJson_WhenReading_ThenDeserialize()
    {
        const string json = @"{
  ""word"": ""hello"",
  ""number"": 42,
  ""nested"": {
    ""setting"": ""value""
  }
}";

        IConfiguration actual = JsonConvert.DeserializeObject<IConfiguration>(json, _serializerSettings);

        Assert.Equal("hello", actual["word"]);
        Assert.Equal("42", actual["number"]);
        Assert.Equal("value", actual["nested:setting"]);
    }

    [Fact]
    public void GivenObject_WhenWrite_ThenSerialize()
    {
        const string expected = @"{
  ""nested"": {
    ""setting"": ""value""
  },
  ""number"": ""42"",
  ""word"": ""hello""
}";

        IConfiguration value = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new KeyValuePair<string, string>[]
                {
                    KeyValuePair.Create("word", "hello"),
                    KeyValuePair.Create("number", "42"),
                    KeyValuePair.Create("nested:setting", "value"),
                })
            .Build();

        string actual = JsonConvert.SerializeObject(value, _serializerSettings);
        Assert.Equal(expected, actual);
    }
}
