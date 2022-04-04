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

public class PartitionedDicomIdentifierJsonConverterTests
{
    private readonly JsonSerializerSettings _serializerSettings;

    public PartitionedDicomIdentifierJsonConverterTests()
    {
        _serializerSettings = new JsonSerializerSettings { Formatting = Formatting.Indented };
        _serializerSettings.Converters.Add(new PartitionedDicomIdentifierJsonConverter());
    }

    [Theory]
    [InlineData(1, "1.2.345", null, null, "1/1.2.345")]
    [InlineData(2, "1.2.345", "67.89", null, "2/1.2.345/67.89")]
    [InlineData(3, "1.2.345", "67.89", "10.11121314.1516.17.18.1920", "3/1.2.345/67.89/10.11121314.1516.17.18.1920")]
    public void GivenJson_WhenReading_ThenDeserialize(int partitionKey, string study, string series, string instance, string value)
    {
        PartitionedDicomIdentifier actual = JsonConvert.DeserializeObject<PartitionedDicomIdentifier>("\"" + value + "\"", _serializerSettings);

        Assert.Equal(partitionKey, actual.PartitionKey);
        Assert.Equal(study, actual.StudyInstanceUid);
        Assert.Equal(series, actual.SeriesInstanceUid);
        Assert.Equal(instance, actual.SopInstanceUid);
    }

    [Theory]
    [InlineData(1, ResourceType.Study, "1.2.345", null, null, "1/1.2.345")]
    [InlineData(2, ResourceType.Series, "1.2.345", "67.89", null, "2/1.2.345/67.89")]
    [InlineData(3, ResourceType.Instance, "1.2.345", "67.89", "10.11121314.1516.17.18.1920", "3/1.2.345/67.89/10.11121314.1516.17.18.1920")]
    public void GivenObject_WhenWrite_ThenSerialize(int partitionKey, ResourceType type, string study, string series, string instance, string value)
    {
        string actual = JsonConvert.SerializeObject(type switch
        {
            ResourceType.Study => new PartitionedDicomIdentifier(DicomIdentifier.ForStudy(study), partitionKey),
            ResourceType.Series => new PartitionedDicomIdentifier(DicomIdentifier.ForSeries(study, series), partitionKey),
            _ => new PartitionedDicomIdentifier(DicomIdentifier.ForInstance(study, series, instance), partitionKey),
        }, _serializerSettings);

        Assert.Equal("\"" + value + "\"", actual);
    }
}
