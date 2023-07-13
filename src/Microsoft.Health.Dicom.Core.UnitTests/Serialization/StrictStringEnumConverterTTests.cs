// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Serialization;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Health.Dicom.Core.UnitTests.Serialization;

public class StrictStringEnumConverterTTests
{
    private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions();

    [Fact]
    public void GivenStrictStringEnumConverter_WhenCheckingNullHandling_ThenReturnFalse()
    {
        Assert.False(new StrictStringEnumConverter<SeekOrigin>().HandleNull);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("\"studys\"")]
    [InlineData("\"innstance\"")]
    [InlineData("42")]
    public void GivenInvalidEnumName_WhenReadingJson_ThenThrowJsonReaderException(string json)
    {
        var jsonReader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

        Assert.True(jsonReader.Read());
        try
        {
            new StrictStringEnumConverter<QueryTagLevel>().Read(ref jsonReader, typeof(QueryTagLevel), DefaultOptions);
            throw ThrowsException.ForNoException(typeof(JsonException));
        }
        catch (Exception e)
        {
            if (e.GetType() != typeof(JsonException))
            {
                // TODO: Update with new method on next version of xUnit - https://github.com/xunit/xunit/issues/2741
                throw ThrowsException.ForNoException(typeof(JsonException));
            }
        }
    }

    [Theory]
    [InlineData("INSTANCE", QueryTagLevel.Instance)]
    [InlineData("series", QueryTagLevel.Series)]
    [InlineData("StUdY", QueryTagLevel.Study)]
    public void GivenStringToken_WhenReadingJson_ThenReturnEnum(string name, QueryTagLevel expected)
    {
        var jsonReader = new Utf8JsonReader(Encoding.UTF8.GetBytes("\"" + name + "\""));

        Assert.True(jsonReader.Read());
        QueryTagLevel actual = new StrictStringEnumConverter<QueryTagLevel>().Read(ref jsonReader, typeof(QueryTagLevel), DefaultOptions);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(QueryTagLevel.Instance, "\"Instance\"")]
    [InlineData(QueryTagLevel.Series, "\"Series\"")]
    [InlineData(QueryTagLevel.Study, "\"Study\"")]
    public void GivenEnumValue_WhenWritingJson_ThenWriteName(QueryTagLevel value, string expected)
    {
        using var buffer = new MemoryStream();
        var jsonWriter = new Utf8JsonWriter(buffer);

        new StrictStringEnumConverter<QueryTagLevel>().Write(jsonWriter, value, DefaultOptions);

        jsonWriter.Flush();
        buffer.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(buffer, Encoding.UTF8);
        Assert.Equal(expected, reader.ReadToEnd());
    }

    [Theory]
    [InlineData(BindingFlags.CreateInstance, "\"createInstance\"")]
    [InlineData(BindingFlags.DoNotWrapExceptions, "\"doNotWrapExceptions\"")]
    [InlineData(BindingFlags.Instance, "\"instance\"")]
    public void GivenNamingPolicy_WhenWritingJson_ThenWriteCamelCase(BindingFlags value, string expected)
    {
        using var buffer = new MemoryStream();
        var jsonWriter = new Utf8JsonWriter(buffer);

        new StrictStringEnumConverter<BindingFlags>(JsonNamingPolicy.CamelCase).Write(jsonWriter, value, DefaultOptions);

        jsonWriter.Flush();
        buffer.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(buffer, Encoding.UTF8);
        Assert.Equal(expected, reader.ReadToEnd());
    }

    [Fact]
    public void GivenUndefinedEnumValue_WhenWritingJson_ThenThrowJsonException()
    {
        using var buffer = new MemoryStream();
        var jsonWriter = new Utf8JsonWriter(buffer);

        Assert.Throws<JsonException>(
            () => new StrictStringEnumConverter<QueryTagLevel>().Write(jsonWriter, (QueryTagLevel)12, DefaultOptions));
    }
}
