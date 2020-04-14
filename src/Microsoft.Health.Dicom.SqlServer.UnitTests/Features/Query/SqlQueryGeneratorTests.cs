// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.SqlServer.Features.Query;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Features.Storage;
using Xunit;

namespace Microsoft.Health.Dicom.SqlServer.UnitTests.Features.Query
{
    public class SqlQueryGeneratorTests
    {
        [Fact]
        public void GivenStudyInstanceUid_WhenIELevelSeries_ValidateDistinctStudySeries()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new DicomQueryIncludeField(false, new List<DicomTag>());
            var filters = new List<DicomQueryFilterCondition>()
            {
                new StringSingleValueMatchCondition(DicomTag.StudyInstanceUID, "1234"),
            };
            var query = new DicomQueryExpression(QueryResource.StudySeries, includeField, false, 0, 0, filters);

            var parm = new SqlQueryParameterManager(CreateSqlParameterCollection());
            new SqlQueryGenerator(stringBuilder, query, parm);

            string expectedDistinctSelect = @"SELECT DISTINCT
i.StudyInstanceUid
,i.SeriesInstanceUid
FROM dbo.Instance i";
            string expectedCrossApply = @"
FROM dbo.Instance a
WHERE 1 = 1
AND a.StudyInstanceUid = f.StudyInstanceUid
AND a.SeriesInstanceUid = f.SeriesInstanceUid";

            Assert.Contains(expectedDistinctSelect, stringBuilder.ToString());
            Assert.Contains(expectedCrossApply, stringBuilder.ToString());
            Assert.Contains("StudyInstanceUid=@p0", stringBuilder.ToString());
            Assert.Contains($"OFFSET 0 ROWS", stringBuilder.ToString());
            Assert.Contains($"FETCH NEXT {DicomQueryLimit.DefaultQueryResultCount} ROWS ONLY", stringBuilder.ToString());
        }

        [Fact]
        public void GivenStudyDate_WhenIELevelSTudy_ValidateDistinctStudyStudies()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new DicomQueryIncludeField(false, new List<DicomTag>());
            var minDate = new DateTime(2020, 2, 1);
            var maxDate = new DateTime(2020, 3, 1);

            var filters = new List<DicomQueryFilterCondition>()
            {
                new DateRangeValueMatchCondition(DicomTag.StudyDate, minDate, maxDate),
            };
            var query = new DicomQueryExpression(QueryResource.AllStudies, includeField, false, 0, 0, filters);

            var parm = new SqlQueryParameterManager(CreateSqlParameterCollection());
            new SqlQueryGenerator(stringBuilder, query, parm);

            string expectedDistinctSelect = @"SELECT DISTINCT
st.StudyInstanceUid
FROM dbo.StudyMetadataCore st";

            string expectedCrossApply = @"
FROM dbo.Instance a
WHERE 1 = 1
AND a.StudyInstanceUid = f.StudyInstanceUid";
            Assert.Contains(expectedDistinctSelect, stringBuilder.ToString());
            Assert.Contains(expectedCrossApply, stringBuilder.ToString());

            Assert.Contains("StudyDate BETWEEN @p0 AND @p1", stringBuilder.ToString());
        }

        [Fact]
        public void GivenSopInstanceUid_WhenIELevelInstance_ValidateDistinctInstances()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new DicomQueryIncludeField(false, new List<DicomTag>());
            var filters = new List<DicomQueryFilterCondition>()
            {
                new StringSingleValueMatchCondition(DicomTag.StudyInstanceUID, "123"),
                new StringSingleValueMatchCondition(DicomTag.SeriesInstanceUID, "456"),
                new StringSingleValueMatchCondition(DicomTag.SOPInstanceUID, "789"),
            };
            var query = new DicomQueryExpression(QueryResource.AllInstances, includeField, false, 0, 0, filters);

            var parm = new SqlQueryParameterManager(CreateSqlParameterCollection());
            new SqlQueryGenerator(stringBuilder, query, parm);

            string expectedDistinctSelect = @"SELECT DISTINCT
i.StudyInstanceUid
,i.SeriesInstanceUid
,i.SopInstanceUid
,i.Watermark
FROM dbo.Instance i";

            string expectedFilters = @"AND i.StudyInstanceUid=@p0
AND i.SeriesInstanceUid=@p1
AND i.SopInstanceUid=@p2";

            Assert.Contains(expectedDistinctSelect, stringBuilder.ToString());
            Assert.Contains(expectedFilters, stringBuilder.ToString());
            Assert.DoesNotContain("CROSS APPLY", stringBuilder.ToString());
        }

        [Fact]
        public void GivenNonUidFilter_WhenIELevelInstance_ValidateDistinctInstances()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new DicomQueryIncludeField(false, new List<DicomTag>());
            var filters = new List<DicomQueryFilterCondition>()
            {
                new StringSingleValueMatchCondition(DicomTag.Modality, "123"),
            };
            var query = new DicomQueryExpression(QueryResource.AllInstances, includeField, false, 0, 0, filters);

            var parm = new SqlQueryParameterManager(CreateSqlParameterCollection());
            new SqlQueryGenerator(stringBuilder, query, parm);

            string expectedDistinctSelect = @"SELECT DISTINCT
st.StudyInstanceUid
,se.SeriesInstanceUid
,i.SopInstanceUid
,i.Watermark
FROM dbo.StudyMetadataCore st
INNER JOIN dbo.SeriesMetadataCore se
ON se.StudyId = st.Id
INNER JOIN dbo.Instance i
ON i.StudyInstanceUid = st.StudyInstanceUid
AND i.SeriesInstanceUid = se.SeriesInstanceUid";

            string expectedFilters = @"AND se.Modality=@p0";

            Assert.Contains(expectedDistinctSelect, stringBuilder.ToString());
            Assert.Contains(expectedFilters, stringBuilder.ToString());
            Assert.DoesNotContain("CROSS APPLY", stringBuilder.ToString());
        }

        [Fact]
        public void GivenPatientNameFilter_WithFuzzyMatchMultiWord_ValidateContainsFilterGenerated()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new DicomQueryIncludeField(false, new List<DicomTag>());
            var filters = new List<DicomQueryFilterCondition>()
            {
                new PersonNameFuzzyMatchCondition(DicomTag.PatientName, "Fall 6"),
            };
            var query = new DicomQueryExpression(QueryResource.AllStudies, includeField, true, 10, 0, filters);
            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm);

            string expectedParam = $"\"Fall*\" AND \"6*\"";

            string expectedFilters = @"AND CONTAINS(PatientNameWords, @p0)";

            Assert.Equal(expectedParam, sqlParameterCollection[0].Value.ToString());
            Assert.Contains(expectedFilters, stringBuilder.ToString());
        }

        private SqlParameterCollection CreateSqlParameterCollection()
        {
            return (SqlParameterCollection)typeof(SqlParameterCollection).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null).Invoke(null);
        }
    }
}
