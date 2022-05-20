// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Models.Common;
using Microsoft.Health.Dicom.Core.Serialization.Newtonsoft;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Serialization.Newtonsoft;

public class DicomIdentifierJsonConverterTests
{
    private readonly JsonSerializerSettings _serializerSettings;

    public DicomIdentifierJsonConverterTests()
    {
        _serializerSettings = new JsonSerializerSettings();
        _serializerSettings.Converters.Add(new DicomIdentifierJsonConverter());
    }

    [Fact]
    public void GivenInvalidToken_WhenReading_ThenThrow()
    {
        Assert.Throws<JsonException>(() => JsonConvert.DeserializeObject<DicomIdentifier>("123", _serializerSettings));
    }

    [Theory]
    [InlineData("\"1.2.345\"", "1.2.345", null, null)]
    [InlineData("\"1.2.345/67.89\"", "1.2.345", "67.89", null)]
    [InlineData("\"1.2.345/67.89/10.11121314.1516.17.18.1920\"", "1.2.345", "67.89", "10.11121314.1516.17.18.1920")]
    public void GivenJson_WhenReading_ThenDeserialize(string json, string study, string series, string instance)
    {
        DicomIdentifier actual = JsonConvert.DeserializeObject<DicomIdentifier>(json, _serializerSettings);

        Assert.Equal(study, actual.StudyInstanceUid);
        Assert.Equal(series, actual.SeriesInstanceUid);
        Assert.Equal(instance, actual.SopInstanceUid);
    }

    [Theory]
    [InlineData("1.2.345", null, null, "\"1.2.345\"")]
    [InlineData("1.2.345", "67.89", null, "\"1.2.345/67.89\"")]
    [InlineData("1.2.345", "67.89", "10.11121314.1516.17.18.1920", "\"1.2.345/67.89/10.11121314.1516.17.18.1920\"")]
    public void GivenDicomIdentifier_WhenConvertingToString_ThenGetString(string study, string series, string instance, string expected)
        => Assert.Equal(expected, JsonConvert.SerializeObject(new DicomIdentifier(study, series, instance), _serializerSettings));
}
