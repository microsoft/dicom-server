// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Extensions
{
    public class DicomTagExtensionsTests
    {
        [Fact]
        public void GivenValidDicomTag_WhenGetPath_ThenShouldReturnCorrectValue()
        {
            Assert.Equal("0014408B", DicomTag.UserSelectedGainY.GetPath());
        }

        [Theory]
        [MemberData(nameof(MemberDataForTestingGetDefaultVR))]
        public void GivenDicomTag_WhenGetDefaultVR_ThenShouldReturnExpectedValue(DicomTag dicomTag, DicomVR expectedVR)
        {
            Assert.Equal(expectedVR, dicomTag.GetDefaultVR());
        }

        public static IEnumerable<object[]> MemberDataForTestingGetDefaultVR()
        {
            yield return new object[] { DicomTag.StudyInstanceUID, DicomVR.UI }; // standard DicomTag
            yield return new object[] { DicomTag.Parse("12051003"), null }; // private DicomTag
            yield return new object[] { DicomTag.Parse("22010010"), DicomVR.LO }; // private identification code
            yield return new object[] { DicomTag.Parse("0018B001"), null }; // invalid DicomTag
        }
    }
}
