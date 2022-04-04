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
        _serializerSettings.Converters.Add(new PartitionedDicomIdentifierJsonConverter());
        _serializerSettings.Converters.Add(new SourceManifestJsonConverter());
        _serializerSettings.Converters.Add(new StringEnumConverter());
    }

    [Fact]
    public void GivenJson_WhenReading_ThenDeserialize()
    {
        const string json = @"{
  ""Type"": ""Identifiers"",
  ""Input"": [
    ""1/1234.5678"",
    ""2/98.765.4/32.1""
  ]
}";

        SourceManifest actual = JsonConvert.DeserializeObject<SourceManifest>(json, _serializerSettings);
        Assert.Equal(ExportSourceType.Identifiers, actual.Type);

        PartitionedDicomIdentifier[] identifiers = actual.Input as PartitionedDicomIdentifier[];
        Assert.NotNull(identifiers);
        Assert.True(identifiers.SequenceEqual(
            new PartitionedDicomIdentifier[]
            {
                new PartitionedDicomIdentifier(DicomIdentifier.ForStudy("1234.5678"),1),
                new PartitionedDicomIdentifier(DicomIdentifier.ForSeries("98.765.4", "32.1"),2),
            }));
    }

    [Fact]
    public void GivenObject_WhenWrite_ThenSerialize()
    {
        const string expected = @"{
  ""Type"": ""Identifiers"",
  ""Input"": [
    ""1/1234.5678"",
    ""2/98.765.4/32.1""
  ]
}";

        var value = new SourceManifest
        {
            Input = new PartitionedDicomIdentifier[]
            {
               new PartitionedDicomIdentifier(DicomIdentifier.ForStudy("1234.5678"),1),
               new PartitionedDicomIdentifier( DicomIdentifier.ForSeries("98.765.4", "32.1"),2),
            },
            Type = ExportSourceType.Identifiers,
        };

        string actual = JsonConvert.SerializeObject(value, _serializerSettings);
        Assert.Equal(expected, actual);
    }
}
