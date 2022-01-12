// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom;
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
        public void GivenValidTag_WhenParse_ThenShouldReturnCorrectValue(string dicomTagPath, DicomTag expectedTag)
        {
            DicomTag[] tags;
            bool succeed = _dicomTagParser.TryParse(dicomTagPath, out tags, supportMultiple: false);
            Assert.True(succeed);
            Assert.Single(tags);
            Assert.Equal(expectedTag, tags[0]);
        }

        [MemberData(nameof(GetInvalidTags))]
        [Theory]
        public void GivenInvalidTag_WhenParse_ThenShouldReturnFalse(string dicomTagPath)
        {
            DicomTag[] tags;
            bool succeed = _dicomTagParser.TryParse(dicomTagPath, out tags, supportMultiple: false);
            Assert.False(succeed);
        }

        [MemberData(nameof(GetValidTags))]
        [Theory]
        public void GivenValidTag_WhenParseToDicomItem_ThenShouldReturnCorrectValue(string dicomTagPath, DicomTag expectedTag)
        {
            DicomItem item;
            bool succeed = _dicomTagParser.TryParseToDicomItem(dicomTagPath, out item);
            Assert.True(succeed);
            Assert.Equal(expectedTag, item.Tag);
        }

        [MemberData(nameof(GetValidMultipleTags))]
        [Theory]
        public void GivenValidMultipleTagPath_WhenParseToDicomItem_ThenShouldReturnCorrectValue(string dicomTagPath, DicomItem expectedItem)
        {
            DicomItem item;
            bool succeed = _dicomTagParser.TryParseToDicomItem(dicomTagPath, out item);
            Assert.True(succeed);
            var sequence = item as DicomSequence;
            var expectedSequence = expectedItem as DicomSequence;
            Assert.Equal(item, expectedItem);
            Assert.Equal(sequence.Items.FirstOrDefault()?.FirstOrDefault(), expectedSequence.Items.FirstOrDefault()?.FirstOrDefault());
        }

        [MemberData(nameof(GetInvalidTags))]
        [Theory]
        public void GivenInValidTag_WhenParseToDicomItem_ThenShouldReturnFalse(string dicomTagPath)
        {
            DicomItem item;
            bool succeed = _dicomTagParser.TryParseToDicomItem(dicomTagPath, out item);
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

        public static IEnumerable<object[]> GetValidMultipleTags()
        {
            yield return new object[] { "0040A370.00080050", new DicomSequence(DicomTag.ReferencedRequestSequence, new DicomDataset[] { new DicomDataset(new DicomValuelessItem(DicomTag.AccessionNumber)) }) }; // ReferencedRequestSequence.Accesionnumber
            yield return new object[] { "0040A370.00401001", new DicomSequence(DicomTag.ReferencedRequestSequence, new DicomDataset[] { new DicomDataset(new DicomValuelessItem(DicomTag.Requested​Procedure​ID)) }) }; // ReferencedRequestSequence.Requested​Procedure​ID
            yield return new object[] { "24010010.12051003", new DicomSequence(DicomTag.Parse("24010010"), new DicomDataset[] { new DicomDataset(new DicomValuelessItem(DicomTag.Parse("12051003"))) }) }; // Private
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
            yield return new object[] { "." }; // delimiter only
            yield return new object[] { "asdasdas.asdasdasd" }; // invalid multiple tags
            yield return new object[] { "0040A370.asdasdasd" }; // valid first level tag and invalid second level tag
        }
    }
}
