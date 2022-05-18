// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Health.Dicom.Core.Models.Common;
using Microsoft.Health.Dicom.Core.Serialization;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Serialization;

public class DicomIdentifierJsonConverterTests
{
    private readonly JsonSerializerOptions _serializerOptions;

    public DicomIdentifierJsonConverterTests()
    {
        _serializerOptions = new JsonSerializerOptions();
        _serializerOptions.Converters.Add(new DicomIdentifierJsonConverter());
    }

    [Fact]
    public void GivenInvalidToken_WhenReading_ThenThrow()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DicomIdentifier>("123", _serializerOptions));
    }

    [Theory]
    [InlineData("\"1.2.345\"", "1.2.345", null, null)]
    [InlineData("\"1.2.345/67.89\"", "1.2.345", "67.89", null)]
    [InlineData("\"1.2.345/67.89/10.11121314.1516.17.18.1920\"", "1.2.345", "67.89", "10.11121314.1516.17.18.1920")]
    public void GivenJson_WhenReading_ThenDeserialize(string json, string study, string series, string instance)
    {
        DicomIdentifier actual = JsonSerializer.Deserialize<DicomIdentifier>(json, _serializerOptions);

        Assert.Equal(study, actual.StudyInstanceUid);
        Assert.Equal(series, actual.SeriesInstanceUid);
        Assert.Equal(instance, actual.SopInstanceUid);
    }

    [Theory]
    [InlineData("1.2.345", null, null, "\"1.2.345\"")]
    [InlineData("1.2.345", "67.89", null, "\"1.2.345/67.89\"")]
    [InlineData("1.2.345", "67.89", "10.11121314.1516.17.18.1920", "\"1.2.345/67.89/10.11121314.1516.17.18.1920\"")]
    public void GivenDicomIdentifier_WhenConvertingToString_ThenGetString(string study, string series, string instance, string expected)
        => Assert.Equal(expected, JsonSerializer.Serialize(new DicomIdentifier(study, series, instance), _serializerOptions));
}
