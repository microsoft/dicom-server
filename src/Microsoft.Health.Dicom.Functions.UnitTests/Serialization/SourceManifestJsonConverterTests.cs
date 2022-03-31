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

public class SourceManifestJsonConverterTests
{
    private readonly JsonSerializerSettings _serializerSettings;

    public SourceManifestJsonConverterTests()
    {
        _serializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
        };

        _serializerSettings.Converters.Add(new StringEnumConverter());
        _serializerSettings.Converters.Add(new SourceManifestJsonConverter());
    }

    [Fact]
    public void GivenJson_WhenReading_ThenDeserialize()
    {
        const string json = @"{
  ""Type"": ""Identifiers"",
  ""Input"": [
    ""1.2.840.10008.1.​1"",
    ""1.2.840.10008.1.​2/1.2.840.10008.1.​2.​1"",
    ""1.2.840.10008.1.2.​1.​99/1.2.840.10008.1.​2.​2/1.2.840.10008.1.2.​4.​50"",
  ]
}";

        SourceManifest actual = JsonConvert.DeserializeObject<SourceManifest>(json, _serializerSettings);
        Assert.Equal(ExportSourceType.Identifiers, actual.Type);

        string[] identifiers = actual.Input as string[];
        Assert.NotNull(identifiers);
        Assert.True(identifiers.SequenceEqual(
            new string[]
            {
                "1.2.840.10008.1.​1",
                "1.2.840.10008.1.​2/1.2.840.10008.1.​2.​1",
                "1.2.840.10008.1.2.​1.​99/1.2.840.10008.1.​2.​2/1.2.840.10008.1.2.​4.​50",
            }));
    }

    [Fact]
    public void GivenObject_WhenWrite_ThenSerialize()
    {
        const string expected = @"{
  ""Type"": ""Identifiers"",
  ""Input"": [
    ""hello"",
    ""world""
  ]
}";

        var value = new SourceManifest
        {
            Input = new string[] { "hello", "world" },
            Type = ExportSourceType.Identifiers,
        };

        string actual = JsonConvert.SerializeObject(value, _serializerSettings);
        Assert.Equal(expected, actual);
    }
}
