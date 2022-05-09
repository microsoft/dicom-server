// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models.Indexing;
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
        Assert.Equal(2, _settings.Converters.Count);
        Assert.Equal(typeof(ConfigurationJsonConverter), _settings.Converters[0].GetType());
        Assert.Equal(typeof(StringEnumConverter), _settings.Converters[1].GetType());

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
  ""Completed"": {
    ""Start"": 1,
    ""End"": 6
  },
  ""CreatedTime"": ""2022-05-09T16:56:48.3050668Z"",
  ""PercentComplete"": 100,
  ""ResourceIds"": [
    ""15"",
    ""16""
  ],
  ""AdditionalProperties"": null,
  ""QueryTagKeys"": [
    15,
    16
  ],
  ""Batching"": {
    ""Size"": 100,
    ""MaxParallelCount"": 1,
    ""MaxParallelElements"": 100
  }
}";

        ReindexCheckpoint checkpoint = JsonConvert.DeserializeObject<ReindexCheckpoint>(json, _settings);
        AssertCheckpoint(checkpoint);

        string actual = JsonConvert.SerializeObject(checkpoint, Formatting.Indented, _settings);
        Assert.Equal(
@"{
  ""completed"": {
    ""start"": 1,
    ""end"": 6
  },
  ""createdTime"": ""2022-05-09T16:56:48.3050668Z"",
  ""queryTagKeys"": [
    15,
    16
  ],
  ""batching"": {
    ""size"": 100,
    ""maxParallelCount"": 1
  }
}",
            actual);

        checkpoint = JsonConvert.DeserializeObject<ReindexCheckpoint>(actual, _settings);
        AssertCheckpoint(checkpoint);
    }

    private static void AssertCheckpoint(ReindexCheckpoint checkpoint)
    {
        var createdTime = DateTime.Parse("2022-05-09T16:56:48.3050668Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        Assert.Null(checkpoint.AdditionalProperties);
        Assert.Equal(100, checkpoint.Batching.Size);
        Assert.Equal(1, checkpoint.Batching.MaxParallelCount);
        Assert.Equal(100, checkpoint.Batching.MaxParallelElements);
        Assert.Equal(new WatermarkRange(1, 6), checkpoint.Completed);
        Assert.Equal(createdTime, checkpoint.CreatedTime);
        Assert.Equal(100, checkpoint.PercentComplete);
        Assert.Equal(2, checkpoint.QueryTagKeys.Count);
        Assert.Contains(15, checkpoint.QueryTagKeys);
        Assert.Contains(16, checkpoint.QueryTagKeys);
        Assert.Equal(2, checkpoint.ResourceIds.Count);
        Assert.Contains("15", checkpoint.ResourceIds);
        Assert.Contains("16", checkpoint.ResourceIds);
    }
}
