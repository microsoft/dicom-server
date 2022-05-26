// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.Common;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Core.Serialization.Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Serialization.Newtonsoft;

public class ExportDataOptionsJsonConverterTests
{
    private readonly JsonSerializerSettings _serializeSettings;

    public ExportDataOptionsJsonConverterTests()
    {
        NamingStrategy camelCase = new CamelCaseNamingStrategy();

        _serializeSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = camelCase },
            Formatting = Formatting.Indented,
        };
        _serializeSettings.Converters.Add(new DicomIdentifierJsonConverter());
        _serializeSettings.Converters.Add(new ExportSourceOptionsJsonConverter(camelCase));
        _serializeSettings.Converters.Add(new ExportDestinationOptionsJsonConverter(camelCase));
        _serializeSettings.Converters.Add(new StringEnumConverter(camelCase));
    }

    [Fact]
    public void GivenNullJson_WhenReading_ThenDeserialize()
    {
        const string json = "null";

        ExportDataOptions<ExportSourceType> actual = JsonConvert.DeserializeObject<ExportDataOptions<ExportSourceType>>(json, _serializeSettings);
        Assert.Null(actual);
    }

    [Fact]
    public void GivenInvalidSourceOptionsJson_WhenReading_ThenDeserialize()
    {
        const string json = @"{
  ""type"": ""identifiers"",
  ""settings"": {
    ""flag"": true,
    ""hello"": [
      ""w"",
      ""o"",
      ""r"",
      ""l"",
      ""d""
    ]
  }
}";

        ExportDataOptions<ExportSourceType> actual = JsonConvert.DeserializeObject<ExportDataOptions<ExportSourceType>>(json, _serializeSettings);
        Assert.Equal(ExportSourceType.Identifiers, actual.Type);

        var options = actual.Settings as IdentifierExportOptions;
        Assert.Null(options.Values);
    }

    [Fact]
    public void GivenInvalidDestinationOptionsJson_WhenReading_ThenDeserialize()
    {
        const string json = @"{
  ""type"": ""azureblob"",
  ""settings"": {
    ""connnectionStrong"":""BlobEndpoint=https://unit-test.blob.core.windows.net/;Foo=Bar"",
    ""blobContainerNamee"": ""mycontainer""
  }
}";

        ExportDataOptions<ExportDestinationType> actual = JsonConvert.DeserializeObject<ExportDataOptions<ExportDestinationType>>(json, _serializeSettings);
        Assert.Equal(ExportDestinationType.AzureBlob, actual.Type);

        var options = actual.Settings as AzureBlobExportOptions;
        Assert.Null(options.ConnectionString);
        Assert.Null(options.BlobContainerName);
        Assert.Null(options.BlobContainerUri);
    }

    [Fact]
    public void GivenSourceOptionsJson_WhenReading_ThenDeserialize()
    {
        const string json = @"{
  ""type"": ""identifiers"",
  ""settings"": {
    ""values"": [
      ""1.2"",
      ""3.4.5/67"",
      ""8.9.10/1.112.13/1.4""
    ]
  }
}";

        ExportDataOptions<ExportSourceType> actual = JsonConvert.DeserializeObject<ExportDataOptions<ExportSourceType>>(json, _serializeSettings);
        Assert.Equal(ExportSourceType.Identifiers, actual.Type);

        var options = actual.Settings as IdentifierExportOptions;
        Assert.Equal(3, options.Values.Count);
        Assert.True(options.Values.SequenceEqual(
            new DicomIdentifier[]
            {
                DicomIdentifier.ForStudy("1.2"),
                DicomIdentifier.ForSeries("3.4.5", "67"),
                DicomIdentifier.ForInstance("8.9.10", "1.112.13", "1.4"),
            }));
    }

    [Fact]
    public void GivenDestinationOptionsJson_WhenReading_ThenDeserialize()
    {
        const string json = @"{
  ""type"": ""azureblob"",
  ""settings"": {
    ""connectionString"": ""BlobEndpoint=https://unit-test.blob.core.windows.net/;Foo=Bar"",
    ""blobContainerName"": ""mycontainer"",
    ""blobContainerUri"": ""https://unit-test.blob.core.windows.net/mycontainer"",
    ""secret"": {
      ""name"": ""foo"",
      ""version"": ""1""
    },
    ""useManagedIdentity"": true
  }
}";

        ExportDataOptions<ExportDestinationType> actual = JsonConvert.DeserializeObject<ExportDataOptions<ExportDestinationType>>(json, _serializeSettings);
        Assert.Equal(ExportDestinationType.AzureBlob, actual.Type);

        var options = actual.Settings as AzureBlobExportOptions;
        Assert.Equal("BlobEndpoint=https://unit-test.blob.core.windows.net/;Foo=Bar", options.ConnectionString);
        Assert.Equal("mycontainer", options.BlobContainerName);
        Assert.Equal(new Uri("https://unit-test.blob.core.windows.net/mycontainer"), options.BlobContainerUri);
        Assert.Equal("foo", options.Secret.Name);
        Assert.Equal("1", options.Secret.Version);
        Assert.True(options.UseManagedIdentity);
    }

    [Fact]
    public void GivenNullValue_WhenWriting_ThenSerialize()
    {
        ExportDataOptions<ExportSourceType> value = null;
        string actual = JsonConvert.SerializeObject(value, _serializeSettings);
        Assert.Equal("null", actual);
    }

    [Fact]
    public void GivenSourceOptions_WhenWriting_ThenSerialize()
    {
        var expected = new ExportDataOptions<ExportSourceType>(
            ExportSourceType.Identifiers,
            new IdentifierExportOptions
            {
                Values = new DicomIdentifier[]
                {
                    DicomIdentifier.ForStudy("1.2"),
                    DicomIdentifier.ForSeries("3.4.5", "67"),
                    DicomIdentifier.ForInstance("8.9.10", "1.112.13", "1.4"),
                }
            });

        string actual = JsonConvert.SerializeObject(expected, _serializeSettings);
        Assert.Equal(
@"{
  ""type"": ""identifiers"",
  ""settings"": {
    ""values"": [
      ""1.2"",
      ""3.4.5/67"",
      ""8.9.10/1.112.13/1.4""
    ]
  }
}",
            actual);
    }

    [Fact]
    public void GivenDestinationOptions_WhenWriting_ThenSerialize()
    {
        var expected = new ExportDataOptions<ExportDestinationType>(
            ExportDestinationType.AzureBlob,
            new AzureBlobExportOptions
            {
                ConnectionString = "BlobEndpoint=https://unit-test.blob.core.windows.net/;Foo=Bar",
                BlobContainerName = "mycontainer",
                BlobContainerUri = new Uri("https://unit-test.blob.core.windows.net/mycontainer"),
                Secret = new SecretKey { Name = "foo", Version = "1" },
                UseManagedIdentity = true,
            });

        string actual = JsonConvert.SerializeObject(expected, _serializeSettings);
        Assert.Equal(
@"{
  ""type"": ""azureBlob"",
  ""settings"": {
    ""blobContainerUri"": ""https://unit-test.blob.core.windows.net/mycontainer"",
    ""connectionString"": ""BlobEndpoint=https://unit-test.blob.core.windows.net/;Foo=Bar"",
    ""blobContainerName"": ""mycontainer"",
    ""useManagedIdentity"": true,
    ""secret"": {
      ""name"": ""foo"",
      ""version"": ""1""
    }
  }
}",
            actual);
    }
}
