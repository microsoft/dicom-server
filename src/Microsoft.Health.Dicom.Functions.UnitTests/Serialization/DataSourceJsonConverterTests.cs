// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Functions.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Serialization;

public class DataSourceJsonConverterTests
{
    private readonly JsonSerializerSettings _serializerSettings;

    public DataSourceJsonConverterTests()
    {
        _serializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
        };

        _serializerSettings.Converters.Add(new StringEnumConverter());
        _serializerSettings.Converters.Add(new DataSourceJsonConverter());
    }

    [Fact]
    public void GivenJson_WhenReading_ThenDeserialize()
    {
        const string json = @"{
  ""Type"": ""UID"",
  ""Metadata"": [
    ""foo"",
    ""bar"",
    ""baz"",
  ]
}";

        DataSource actual = JsonConvert.DeserializeObject<DataSource>(json, _serializerSettings);
        Assert.Equal(ExportSourceType.UID, actual.Type);

        string[] identifiers = actual.Metadata as string[];
        Assert.NotNull(identifiers);
        Assert.True(identifiers.SequenceEqual(new string[] { "foo", "bar", "baz" }));
    }

    [Fact]
    public void GivenObject_WhenWrite_ThenSerialize()
    {
        const string expected = @"{
  ""Type"": ""UID"",
  ""Metadata"": [
    ""hello"",
    ""world""
  ]
}";

        var value = new DataSource
        {
            Metadata = new string[] { "hello", "world" },
            Type = ExportSourceType.UID,
        };

        string actual = JsonConvert.SerializeObject(value, _serializerSettings);
        Assert.Equal(expected, actual);
    }
}
