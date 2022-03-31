// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Core.Serialization.Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Serialization.Newtonsoft;

public class SourceManifestJsonConverterTests
{
    private readonly JsonSerializerSettings _serializerSettings;

    public SourceManifestJsonConverterTests()
    {
        _serializerSettings = new JsonSerializerSettings { Formatting = Formatting.Indented };
        _serializerSettings.Converters.Add(new DicomIdentifierJsonConverter());
        _serializerSettings.Converters.Add(new SourceManifestJsonConverter());
        _serializerSettings.Converters.Add(new StringEnumConverter());
    }

    [Fact]
    public void GivenJson_WhenReading_ThenDeserialize()
    {
        const string json = @"{
  ""Type"": ""Identifiers"",
  ""Input"": [
    ""1234.5678"",
    ""98.765.4/32.1""
  ]
}";

        SourceManifest actual = JsonConvert.DeserializeObject<SourceManifest>(json, _serializerSettings);
        Assert.Equal(ExportSourceType.Identifiers, actual.Type);

        DicomIdentifier[] identifiers = actual.Input as DicomIdentifier[];
        Assert.NotNull(identifiers);
        Assert.True(identifiers.SequenceEqual(
            new DicomIdentifier[]
            {
                DicomIdentifier.ForStudy("1234.5678"),
                DicomIdentifier.ForSeries("98.765.4", "32.1"),
            }));
    }

    [Fact]
    public void GivenObject_WhenWrite_ThenSerialize()
    {
        const string expected = @"{
  ""Type"": ""Identifiers"",
  ""Input"": [
    ""1234.5678"",
    ""98.765.4/32.1""
  ]
}";

        var value = new SourceManifest
        {
            Input = new DicomIdentifier[]
            {
                DicomIdentifier.ForStudy("1234.5678"),
                DicomIdentifier.ForSeries("98.765.4", "32.1"),
            },
            Type = ExportSourceType.Identifiers,
        };

        string actual = JsonConvert.SerializeObject(value, _serializerSettings);
        Assert.Equal(expected, actual);
    }
}
