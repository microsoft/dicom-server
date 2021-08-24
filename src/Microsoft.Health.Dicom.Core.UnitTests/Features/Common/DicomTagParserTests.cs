// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Common
{
    public class DicomTagParserTests
    {
        [MemberData(nameof(GetValidTags))]
        [Theory]
        public void GivenValidTag_WhenParse_ThenShouldReturnCorrectValue(string dicomTagPath, DicomTag expectedTag)
        {
            DicomTag[] tags;
            bool succeed = DicomTagParser.TryParse(dicomTagPath, out tags, supportMultiple: false);
            Assert.True(succeed);
            Assert.Single(tags);
            Assert.Equal(tags[0], expectedTag);
        }

        [MemberData(nameof(GetInvalidTags))]
        [Theory]
        public void GivenInvalidTag_WhenParse_ThenShouldReturnFalse(string dicomTagPath)
        {
            DicomTag[] tags;
            bool succeed = DicomTagParser.TryParse(dicomTagPath, out tags, supportMultiple: false);
            Assert.False(succeed);
        }

        public static IEnumerable<object[]> GetValidTags()
        {
            yield return new object[] { DicomTag.AcquisitionDateTime.GetPath(), DicomTag.AcquisitionDateTime }; // attribute id
            yield return new object[] { DicomTag.AcquisitionDateTime.GetPath().ToLowerInvariant(), DicomTag.AcquisitionDateTime }; // attribute id on lower case
            yield return new object[] { DicomTag.AcquisitionDateTime.DictionaryEntry.Keyword, DicomTag.AcquisitionDateTime }; // keyword
            yield return new object[] { "12051003", DicomTag.Parse("12051003") }; // private tag
            yield return new object[] { "24010010", DicomTag.Parse("24010010") }; // Private Identification code
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
        }
    }
}
