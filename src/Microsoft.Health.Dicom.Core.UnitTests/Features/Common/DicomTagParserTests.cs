// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Common
{
    public class DicomTagParserTests
    {
        private readonly IDicomTagParser _dicomTagParser;

        public DicomTagParserTests()
        {
            _dicomTagParser = new DicomTagParser();
        }

        [MemberData(nameof(GetValidTags))]
        [Theory]
        public void GivenValidTag_WhenParse_ThenShouldReturnCorrectValue(string dicomTagPath, DicomTag[] expectedTags, bool supportMultiple = false)
        {
            DicomTag[] tags;
            bool succeed = _dicomTagParser.TryParse(dicomTagPath, out tags, supportMultiple);
            Assert.True(succeed);
            for (int i = 0; i < expectedTags.Length; i++)
            {
                Assert.True(expectedTags[i].Equals(tags[i]));
            }
        }

        [MemberData(nameof(GetInvalidTags))]
        [Theory]
        public void GivenInvalidTag_WhenParse_ThenShouldReturnFalse(string dicomTagPath, bool supportMultiple = false)
        {
            DicomTag[] tags;
            bool succeed = _dicomTagParser.TryParse(dicomTagPath, out tags, supportMultiple);
            Assert.False(succeed);
        }

        public static IEnumerable<object[]> GetValidTags()
        {
            yield return new object[] { DicomTag.AcquisitionDateTime.GetPath(), new DicomTag[] { DicomTag.AcquisitionDateTime } }; // attribute id
            yield return new object[] { DicomTag.AcquisitionDateTime.GetPath().ToLowerInvariant(), new DicomTag[] { DicomTag.AcquisitionDateTime } }; // attribute id on lower case
            yield return new object[] { DicomTag.AcquisitionDateTime.DictionaryEntry.Keyword, new DicomTag[] { DicomTag.AcquisitionDateTime } }; // keyword
            yield return new object[] { "12051003", new DicomTag[] { DicomTag.Parse("12051003") } }; // private tag
            yield return new object[] { "24010010", new DicomTag[] { DicomTag.Parse("24010010") } }; // Private Identification code
            yield return new object[] { "0040A370.00080050", new DicomTag[] { DicomTag.ReferencedRequestSequence, DicomTag.AccessionNumber }, true }; // ReferencedRequestSequence.Accesionnumber
            yield return new object[] { "0040A370.00401001", new DicomTag[] { DicomTag.ReferencedRequestSequence, DicomTag.Requested​Procedure​ID }, true }; // ReferencedRequestSequence.Requested​Procedure​ID
            yield return new object[] { "24010010.12051003", new DicomTag[] { DicomTag.Parse("24010010"), DicomTag.Parse("12051003") }, true }; // Private
        }

        public static IEnumerable<object[]> GetInvalidTags()
        {
            yield return new object[] { string.Empty }; // empty
            yield return new object[] { null }; // attribute id on lower case
            yield return new object[] { DicomTag.AcquisitionDateTime.DictionaryEntry.Keyword.ToLowerInvariant() }; // keyword in lower case
            yield return new object[] { "0018B001" }; // unknown tag
            yield return new object[] { "0018B001A1" }; // longer than 8.
            yield return new object[] { "Unknown" }; // bug https://microsofthealth.visualstudio.com/Health/_workitems/edit/80766
            yield return new object[] { "PrivateCreator" }; // Key word to Private Identification code.
            yield return new object[] { ".", true }; // delimiter only
            yield return new object[] { "asdasdas.asdasdasd", true }; // invalid multiple tags
            yield return new object[] { "0040A370.asdasdasd", true }; // valid first level tag and invalid second level tag
        }
    }
}
