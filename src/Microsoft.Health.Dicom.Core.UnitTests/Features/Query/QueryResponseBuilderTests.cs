// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Query
{
    public class QueryResponseBuilderTests
    {
        [Fact]
        public void GivenStudyLevel_WithIncludeField_ValidReturned()
        {
            var includeField = new DicomQueryIncludeField(false, new List<DicomTag>() { DicomTag.StudyDescription, DicomTag.Modality });
            var filters = new List<DicomQueryFilterCondition>()
            {
                new StringSingleValueMatchCondition(DicomTag.PatientAge, "35"),
            };
            var query = new DicomQueryExpression(QueryResource.AllStudies, includeField, false, 0, 0, filters);

            DicomDataset responseDataset = QueryResponseBuilder.GenerateResponseDataset(GenerateTestDataSet(), query);

            Assert.True(responseDataset.Contains(DicomTag.StudyInstanceUID)); // Default
            Assert.True(responseDataset.Contains(DicomTag.PatientAge)); // Match condition
            Assert.True(responseDataset.Contains(DicomTag.StudyDescription)); // Valid include
            Assert.False(responseDataset.Contains(DicomTag.Modality)); // Invalid include
            Assert.False(responseDataset.Contains(DicomTag.SeriesInstanceUID)); // Invalid study resource
            Assert.False(responseDataset.Contains(DicomTag.SOPInstanceUID)); // Invalid study resource
        }

        [Fact]
        public void GivenStudySeriesLevel_WithIncludeField_ValidReturned()
        {
            var includeField = new DicomQueryIncludeField(false, new List<DicomTag>() { DicomTag.StudyDescription, DicomTag.Modality });
            var filters = new List<DicomQueryFilterCondition>()
            {
                new StringSingleValueMatchCondition(DicomTag.StudyInstanceUID, "35"),
            };
            var query = new DicomQueryExpression(QueryResource.StudySeries, includeField, false, 0, 0, filters);

            DicomDataset responseDataset = QueryResponseBuilder.GenerateResponseDataset(GenerateTestDataSet(), query);

            Assert.True(responseDataset.Contains(DicomTag.StudyInstanceUID)); // Valid filter
            Assert.True(responseDataset.Contains(DicomTag.StudyDescription)); // Valid include
            Assert.True(responseDataset.Contains(DicomTag.Modality)); // Valid include
            Assert.True(responseDataset.Contains(DicomTag.SeriesInstanceUID)); // Valid Series resource
            Assert.False(responseDataset.Contains(DicomTag.SOPInstanceUID)); // Invalid Series resource
        }

        [Fact]
        public void GivenAllSeriesLevel_WithIncludeField_ValidReturned()
        {
            var includeField = new DicomQueryIncludeField(true, new List<DicomTag>() { });
            var filters = new List<DicomQueryFilterCondition>();
            var query = new DicomQueryExpression(QueryResource.AllSeries, includeField, false, 0, 0, filters);

            DicomDataset responseDataset = QueryResponseBuilder.GenerateResponseDataset(GenerateTestDataSet(), query);

            Assert.True(responseDataset.Contains(DicomTag.StudyInstanceUID)); // Valid study field
            Assert.True(responseDataset.Contains(DicomTag.StudyDescription)); // Valid all study field
            Assert.True(responseDataset.Contains(DicomTag.Modality)); // Valid series field
            Assert.True(responseDataset.Contains(DicomTag.SeriesInstanceUID)); // Valid Series resource
            Assert.False(responseDataset.Contains(DicomTag.SOPInstanceUID)); // Invalid Series resource
        }

        [Fact]
        public void GivenAllInstanceLevel_WithIncludeField_ValidReturned()
        {
            var includeField = new DicomQueryIncludeField(true, new List<DicomTag>() { });
            var filters = new List<DicomQueryFilterCondition>();
            var query = new DicomQueryExpression(QueryResource.AllInstances, includeField, false, 0, 0, filters);

            DicomDataset responseDataset = QueryResponseBuilder.GenerateResponseDataset(GenerateTestDataSet(), query);

            Assert.True(responseDataset.Contains(DicomTag.StudyInstanceUID)); // Valid study field
            Assert.True(responseDataset.Contains(DicomTag.StudyDescription)); // Valid all study field
            Assert.True(responseDataset.Contains(DicomTag.Modality)); // Valid instance field
            Assert.True(responseDataset.Contains(DicomTag.SeriesInstanceUID)); // Valid instance resource
            Assert.True(responseDataset.Contains(DicomTag.SOPInstanceUID)); // Valid instance resource
        }

        [Fact]
        public void GivenStudyInstanceLevel_WithIncludeField_ValidReturned()
        {
            var includeField = new DicomQueryIncludeField(false, new List<DicomTag>() { DicomTag.Modality });
            var filters = new List<DicomQueryFilterCondition>()
            {
                new StringSingleValueMatchCondition(DicomTag.StudyInstanceUID, "35"),
            };
            var query = new DicomQueryExpression(QueryResource.StudyInstances, includeField, false, 0, 0, filters);

            DicomDataset responseDataset = QueryResponseBuilder.GenerateResponseDataset(GenerateTestDataSet(), query);

            Assert.True(responseDataset.Contains(DicomTag.StudyInstanceUID)); // Valid filter
            Assert.False(responseDataset.Contains(DicomTag.StudyDescription)); // StudyInstance does not include study tags by deault
            Assert.True(responseDataset.Contains(DicomTag.Modality)); // Valid series field
            Assert.True(responseDataset.Contains(DicomTag.SeriesInstanceUID)); // Valid series tag
            Assert.True(responseDataset.Contains(DicomTag.SOPInstanceUID)); // Valid instance tag
        }

        [Fact]
        public void GivenStudySeriesInstanceLevel_WithIncludeField_ValidReturned()
        {
            var includeField = new DicomQueryIncludeField(false, new List<DicomTag>() { });
            var filters = new List<DicomQueryFilterCondition>()
            {
                new StringSingleValueMatchCondition(DicomTag.StudyInstanceUID, "35"),
                new StringSingleValueMatchCondition(DicomTag.SeriesInstanceUID, "351"),
            };
            var query = new DicomQueryExpression(QueryResource.StudySeriesInstances, includeField, false, 0, 0, filters);

            DicomDataset responseDataset = QueryResponseBuilder.GenerateResponseDataset(GenerateTestDataSet(), query);

            Assert.True(responseDataset.Contains(DicomTag.StudyInstanceUID)); // Valid filter
            Assert.False(responseDataset.Contains(DicomTag.StudyDescription)); // StudySeriesInstance does not include study tags by deault
            Assert.False(responseDataset.Contains(DicomTag.Modality)); // StudySeriesInstance does not include series tags by deault
            Assert.True(responseDataset.Contains(DicomTag.SeriesInstanceUID)); // Valid series tag
            Assert.True(responseDataset.Contains(DicomTag.SOPInstanceUID)); // Valid instance tag
        }

        private DicomDataset GenerateTestDataSet()
        {
            return new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.SeriesInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.SOPInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.PatientAge, "035Y" },
                { DicomTag.StudyDescription, "CT scan" },
                { DicomTag.Modality, "CT" },
            };
        }
    }
}
