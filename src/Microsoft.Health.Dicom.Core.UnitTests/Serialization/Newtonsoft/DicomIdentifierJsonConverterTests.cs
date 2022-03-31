// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Serialization.Newtonsoft;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Serialization.Newtonsoft;

public class DicomIdentifierJsonConverterTests
{
    private readonly JsonSerializerSettings _serializerSettings;

    public DicomIdentifierJsonConverterTests()
    {
        _serializerSettings = new JsonSerializerSettings { Formatting = Formatting.Indented };
        _serializerSettings.Converters.Add(new DicomIdentifierJsonConverter());
    }

    [Theory]
    [InlineData("1.2.345", null, null, "1.2.345")]
    [InlineData("1.2.345", "67.89", null, "1.2.345/67.89")]
    [InlineData("1.2.345", "67.89", "10.11121314.1516.17.18.1920", "1.2.345/67.89/10.11121314.1516.17.18.1920")]
    public void GivenJson_WhenReading_ThenDeserialize(string study, string series, string instance, string value)
    {
        DicomIdentifier actual = JsonConvert.DeserializeObject<DicomIdentifier>("\"" + value + "\"", _serializerSettings);

        Assert.Equal(study, actual.StudyInstanceUid);
        Assert.Equal(series, actual.SeriesInstanceUid);
        Assert.Equal(instance, actual.SopInstanceUid);
    }

    [Theory]
    [InlineData(ResourceType.Study, "1.2.345", null, null, "1.2.345")]
    [InlineData(ResourceType.Series, "1.2.345", "67.89", null, "1.2.345/67.89")]
    [InlineData(ResourceType.Instance, "1.2.345", "67.89", "10.11121314.1516.17.18.1920", "1.2.345/67.89/10.11121314.1516.17.18.1920")]
    public void GivenObject_WhenWrite_ThenSerialize(ResourceType type, string study, string series, string instance, string value)
    {
        string actual = JsonConvert.SerializeObject(type switch
        {
            ResourceType.Study => DicomIdentifier.ForStudy(study),
            ResourceType.Series => DicomIdentifier.ForSeries(study, series),
            _ => DicomIdentifier.ForInstance(study, series, instance),
        }, _serializerSettings);

        Assert.Equal("\"" + value + "\"", actual);
    }
}
