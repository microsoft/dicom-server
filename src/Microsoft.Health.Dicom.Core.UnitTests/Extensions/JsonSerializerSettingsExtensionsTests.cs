// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Serialization.Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Extensions;

public class JsonSerializerSettingsExtensionsTests
{
    private readonly JsonSerializerSettings _settings;

    public JsonSerializerSettingsExtensionsTests()
    {
        _settings = new JsonSerializerSettings();
        _settings.ConfigureDefaultDicomSettings();
    }

    [Fact]
    public void GivenSettings_WhenConfiguringDefaults_ThenUpdateProperties()
    {
        Assert.Equal(4, _settings.Converters.Count);
        Assert.Equal(typeof(DicomIdentifierJsonConverter), _settings.Converters[0].GetType());
        Assert.Equal(typeof(ExportDestinationOptionsJsonConverter), _settings.Converters[1].GetType());
        Assert.Equal(typeof(ExportSourceOptionsJsonConverter), _settings.Converters[2].GetType());
        Assert.Equal(typeof(StringEnumConverter), _settings.Converters[3].GetType());

        Assert.Equal(DateParseHandling.None, _settings.DateParseHandling);
        Assert.Equal(TypeNameHandling.None, _settings.TypeNameHandling);
        Assert.IsType<DefaultContractResolver>(_settings.ContractResolver);
        Assert.IsType<CamelCaseNamingStrategy>(((DefaultContractResolver)_settings.ContractResolver).NamingStrategy);
    }

    [Fact]
    public void GivenOldJson_WhenDeserializingWithNewSettings_ThenPreserveProperties()
    {
        const string json =
@"{
  ""Timestamp"": ""2022-05-09T16:56:48.3050668Z"",
  ""Options"": {
    ""Enabled"": true,
    ""Number"": 42,
    ""Words"": [
      ""foo"",
      ""bar"",
      ""baz""
    ],
    ""Extra"": null,
  },
  ""Href"": ""https://www.bing.com""
}";

        Checkpoint checkpoint = JsonConvert.DeserializeObject<Checkpoint>(json, _settings);
        AssertCheckpoint(checkpoint);

        string actual = JsonConvert.SerializeObject(checkpoint, Formatting.Indented, _settings);
        Assert.Equal(
@"{
  ""timestamp"": ""2022-05-09T16:56:48.3050668Z"",
  ""options"": {
    ""enabled"": true,
    ""number"": 42,
    ""words"": [
      ""foo"",
      ""bar"",
      ""baz""
    ]
  },
  ""href"": ""https://www.bing.com""
}",
        actual);

        checkpoint = JsonConvert.DeserializeObject<Checkpoint>(actual, _settings);
        AssertCheckpoint(checkpoint);
    }

    private static void AssertCheckpoint(Checkpoint checkpoint)
    {
        var timestamp = DateTime.Parse("2022-05-09T16:56:48.3050668Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        Assert.Equal(timestamp, checkpoint.Timestamp);
        Assert.Equal(new Uri("https://www.bing.com"), checkpoint.Href);
        Assert.True(checkpoint.Options.Enabled);
        Assert.Equal(42, checkpoint.Options.Number);
        Assert.True(checkpoint.Options.Words.SequenceEqual(new string[] { "foo", "bar", "baz" }));
    }

    private sealed class Checkpoint
    {
        public DateTime Timestamp { get; set; }

        public Options Options { get; set; }

        public Uri Href { get; set; }
    }

    private sealed class Options
    {
        public bool Enabled { get; set; }

        public int Number { get; set; }

        public IReadOnlyList<string> Words { get; set; }

        public object Extra { get; set; }
    }
}
