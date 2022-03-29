// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Core.Serialization;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Serialization;

public class DataSourceConverterFactoryTests
{
    private readonly JsonSerializerOptions _defaultOptions;

    public DataSourceConverterFactoryTests()
    {
        _defaultOptions = new JsonSerializerOptions();
        _defaultOptions.Converters.Add(new DataSourceJsonConverter());
    }

    [Fact]
    public void GivenInvalidTypes_WhenCheckingCanConvert_ThenReturnFalse()
    {
        var factory = new DataSourceJsonConverterFactory();
        Assert.False(factory.CanConvert(typeof(int)));
    }

    [Fact]
    public void GivenValidTypes_WhenCheckingCanConvert_ThenReturnTrue()
    {
        var factory = new DataSourceJsonConverterFactory();
        Assert.True(factory.CanConvert(typeof(DataSource)));
    }

    [Fact]
    public void GivenValidValue_WhenSerializeAndDeserialize_ThenShouldSucceed()
    {
        UidsSource uidSource = new UidsSource()
        {
            Uids = new string[] { "1.2", "2/3" }
        };
        DataSource source = new DataSource()
        {
            Type = ExportSourceType.UID,
            Metadata = uidSource
        };

        string serialized = JsonSerializer.Serialize(source, _defaultOptions);
        Assert.Equal("{\"Type\":\"UID\",\"Metadata\":{\"Uids\":[\"1.2\",\"2/3\"]}}", serialized);

        DataSource deserialized = JsonSerializer.Deserialize<DataSource>(serialized, _defaultOptions);
        Assert.Equal(ExportSourceType.UID, deserialized.Type);
        Assert.Equal(uidSource.Uids, ((UidsSource)deserialized.Metadata).Uids);

    }
}
