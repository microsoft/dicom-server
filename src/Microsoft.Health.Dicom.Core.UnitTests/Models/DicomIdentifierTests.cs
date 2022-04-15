// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------


using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Models.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Models;

public class DicomIdentifierTests
{
    [Theory]
    [InlineData("1.2.345", null, null, ResourceType.Study)]
    [InlineData("1.2.345", "67.89", null, ResourceType.Series)]
    [InlineData("1.2.345", "67.89", "10.11121314.1516.17.18.1920", ResourceType.Instance)]
    public void GivenDicomIdentifier_WhenQueryingType_ThenMapType(string study, string series, string instance, ResourceType expected)
        => Assert.Equal(expected, new DicomIdentifier(study, series, instance).Type);

    [Theory]
    [InlineData("1.2.345", null, null, "1.2.345")]
    [InlineData("1.2.345", "67.89", null, "1.2.345/67.89")]
    [InlineData("1.2.345", "67.89", "10.11121314.1516.17.18.1920", "1.2.345/67.89/10.11121314.1516.17.18.1920")]
    public void GivenValidString_WhenParsing_ThenGetDicomIdentifier(string study, string series, string instance, string value)
    {
        var actual = DicomIdentifier.Parse(value);

        Assert.Equal(study, actual.StudyInstanceUid);
        Assert.Equal(series, actual.SeriesInstanceUid);
        Assert.Equal(instance, actual.SopInstanceUid);
    }

    [Theory]
    [InlineData("1.2.345", null, null, "1.2.345")]
    [InlineData("1.2.345", "67.89", null, "1.2.345/67.89")]
    [InlineData("1.2.345", "67.89", "10.11121314.1516.17.18.1920", "1.2.345/67.89/10.11121314.1516.17.18.1920")]
    public void GivenDicomIdentifier_WhenConvertingToString_ThenGetString(string study, string series, string instance, string expected)
        => Assert.Equal(expected, new DicomIdentifier(study, series, instance).ToString());
}
