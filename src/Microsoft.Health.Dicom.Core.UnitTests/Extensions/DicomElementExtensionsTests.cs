// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Extensions;

public class DicomElementExtensionsTests
{

    [Fact]
    public void GivenDicomElementWithMultipleValue_WhenCallGetFirstValueOrDefault_ThenShouldReturnFirstOne()
    {
        DicomElement element = new DicomLongString(DicomTag.StudyDescription, "Value1", "Value2");
        Assert.Equal("Value1", element.GetFirstValueOrDefault<string>());
    }

    [Fact]
    public void GivenDicomElementWithSingleValue_WhenCallGetFirstValueOrDefault_ThenShouldReturnFirstOne()
    {
        DicomElement element = new DicomSignedShort(DicomTag.ExposureControlSensingRegionLeftVerticalEdge, 1);
        Assert.Equal(1, element.GetFirstValueOrDefault<short>());
    }

    [Fact]
    public void GivenDicomElementWithoutValues_WhenCallGetFirstValueOrDefault_ThenShouldReturnDefault()
    {
        DicomElement element = new DicomLongString(DicomTag.StudyDescription, string.Empty);
        Assert.Null(element.GetFirstValueOrDefault<string>());
    }
}
