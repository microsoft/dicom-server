// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.Serialization;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Serialization;

public class DicomIdentifierJsonConverterTests
{
    private readonly JsonSerializerSettings _serializerSettings;

    public DicomIdentifierJsonConverterTests()
    {
        _serializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
        };

        _serializerSettings.Converters.Add(new DicomIdentifierJsonConverter());
    }

    [Theory]
    [InlineData("1.2.840.10008.1.1", null, null, "1.2.840.10008.1.1")]
    [InlineData("1.2.840.10008.1.2", "1.2.840.10008.1.2.1", null, "1.2.840.10008.1.2/1.2.840.10008.1.2.1")]
    [InlineData("1.2.840.10008.1.2.1.99", "1.2.840.10008.1.2.2", "1.2.840.10008.1.2.4.50", "1.2.840.10008.1.2.1.99/1.2.840.10008.1.2.2/1.2.840.10008.1.2.4.50")]
    public void GivenJson_WhenReading_ThenDeserialize(string study, string series, string instance, string value)
    {
        DicomIdentifier actual = JsonConvert.DeserializeObject<DicomIdentifier>("\"" + value + "\"", _serializerSettings);

        Assert.Equal(study, actual.StudyInstanceUid);
        Assert.Equal(series, actual.SeriesInstanceUid);
        Assert.Equal(instance, actual.SopInstanceUid);
    }

    [Fact]
    public void GivenObject_WhenWrite_ThenSerialize()
    {
    }
}
