// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Dicom;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.SqlServer.Features.Query;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Features.Storage;
using Xunit;

namespace Microsoft.Health.Dicom.SqlServer.UnitTests.Features.Query
{
    public class SqlQueryGeneratorTests
    {
        [Fact]
        public void GivenStudyDate_WhenIELevelStudy_ValidateDistinctStudyStudies()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(false, new List<DicomTag>());
            var minDate = new DateTime(2020, 2, 1);
            var maxDate = new DateTime(2020, 3, 1);

            var filters = new List<QueryFilterCondition>()
            {
                new DateRangeValueMatchCondition(DicomTag.StudyDate, minDate, maxDate),
            };
            var query = new QueryExpression(QueryResource.AllStudies, includeField, false, 0, 0, filters);

            var parm = new SqlQueryParameterManager(CreateSqlParameterCollection());
            new SqlQueryGenerator(stringBuilder, query, parm);

            string expectedDistinctSelect = @"SELECT 
st.StudyKey
FROM dbo.Study st";

            string expectedCrossApply = @"
FROM dbo.Instance a
WHERE 1 = 1
AND a.StudyKey = f.StudyKey";
            Assert.Contains(expectedDistinctSelect, stringBuilder.ToString());
            Assert.Contains(expectedCrossApply, stringBuilder.ToString());

            Assert.Contains("StudyDate BETWEEN @p0 AND @p1", stringBuilder.ToString());
        }

        [Fact]
        public void GivenNonUidFilter_WhenIELevelInstance_ValidateDistinctInstances()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(false, new List<DicomTag>());
            var filters = new List<QueryFilterCondition>()
            {
                new StringSingleValueMatchCondition(DicomTag.Modality, "123"),
            };
            var query = new QueryExpression(QueryResource.AllInstances, includeField, false, 0, 0, filters);

            var parm = new SqlQueryParameterManager(CreateSqlParameterCollection());
            new SqlQueryGenerator(stringBuilder, query, parm);

            string expectedDistinctSelect = @"SELECT 
i.StudyInstanceUid
,i.SeriesInstanceUid
,i.SopInstanceUid
,i.Watermark
FROM dbo.Study st
INNER JOIN dbo.Series se
ON se.StudyKey = st.StudyKey
INNER JOIN dbo.Instance i
ON i.SeriesKey = se.SeriesKey";

            string expectedFilters = @"AND se.Modality=@p0";

            Assert.Contains(expectedDistinctSelect, stringBuilder.ToString());
            Assert.Contains(expectedFilters, stringBuilder.ToString());
            Assert.DoesNotContain("CROSS APPLY", stringBuilder.ToString());
        }

        [Fact]
        public void GivenStudyCustomTagFilter_WhenIELevelStudy_ValidateCustomTagFilter()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(false, new List<DicomTag>());
            var filter = new StringSingleValueMatchCondition(DicomTag.ModelGroupUID, "123");
            var filterDetails = new CustomTagFilterDetails(1, CustomTagLevel.Study, DicomTag.ModelGroupUID);
            filter.CustomTagFilterDetails = filterDetails;
            var filters = new List<QueryFilterCondition>()
            {
                filter,
            };
            var query = new QueryExpression(QueryResource.AllStudies, includeField, false, 0, 0, filters, new HashSet<CustomTagFilterDetails>() { filterDetails });

            var parm = new SqlQueryParameterManager(CreateSqlParameterCollection());
            new SqlQueryGenerator(stringBuilder, query, parm);

            string expectedCustomTagTableFilter = @"INNER JOIN dbo.CustomTagString cts1
ON cts1.StudyKey = st.StudyKey
WHERE";

            string expectedFilters = @"AND cts1.TagKey=@p0
AND cts1.TagValue=@p1";

            string builtString = stringBuilder.ToString();
            Assert.Contains(expectedCustomTagTableFilter, builtString);
            Assert.Contains(expectedFilters, builtString);
        }

        [Fact]
        public void GivenCustomTagFilterWithNonUidFilter_WhenIELevelSeries_ValidateCustomTagFilter()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(false, new List<DicomTag>());
            var customTagFilter = new StringSingleValueMatchCondition(DicomTag.ModelGroupUID, "123");
            var filterDetails = new CustomTagFilterDetails(1, CustomTagLevel.Instance, DicomTag.ModelGroupUID);
            customTagFilter.CustomTagFilterDetails = filterDetails;
            var filter = new StringSingleValueMatchCondition(DicomTag.Modality, "abc");
            var filters = new List<QueryFilterCondition>()
            {
                filter,
                customTagFilter,
            };
            var query = new QueryExpression(QueryResource.StudySeries, includeField, false, 0, 0, filters, new HashSet<CustomTagFilterDetails>() { filterDetails });

            var parm = new SqlQueryParameterManager(CreateSqlParameterCollection());
            new SqlQueryGenerator(stringBuilder, query, parm);

            string expectedCustomTagTableFilter = @"INNER JOIN dbo.CustomTagString cts1
ON cts1.StudyKey = st.StudyKey
AND ON cts1.SeriesKey = se.SeriesKey
WHERE";

            string expectedFilter = @"AND se.Modality=@p0";
            string expectedCustomTagFilter = @"AND cts1.TagKey=@p1
AND cts1.TagValue=@p2";

            string builtString = stringBuilder.ToString();
            Assert.Contains(expectedCustomTagTableFilter, builtString);
            Assert.Contains(expectedFilter, builtString);
            Assert.Contains(expectedCustomTagFilter, builtString);
        }

        [Fact]
        public void GivenMultipleCustomTagFilters_WhenIELevelInstance_ValidateCustomTagFilter()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(false, new List<DicomTag>());
            var filter1 = new StringSingleValueMatchCondition(DicomTag.ModelGroupUID, "abc");
            var filterDetails1 = new CustomTagFilterDetails(1, CustomTagLevel.Instance, DicomTag.ModelGroupUID);
            filter1.CustomTagFilterDetails = filterDetails1;
            var filter2 = new StringSingleValueMatchCondition(DicomTag.ContainerDescription, "description");
            var filterDetails2 = new CustomTagFilterDetails(2, CustomTagLevel.Series, DicomTag.ContainerDescription);
            filter2.CustomTagFilterDetails = filterDetails2;
            var filter3 = new LongSingleValueMatchCondition(DicomTag.NumberOfAssessmentObservations, 123);
            var filterDetails3 = new CustomTagFilterDetails(4, CustomTagLevel.Study, DicomTag.NumberOfAssessmentObservations);
            filter3.CustomTagFilterDetails = filterDetails3;
            var filters = new List<QueryFilterCondition>()
            {
                filter1,
                filter2,
                filter3,
            };
            var query = new QueryExpression(QueryResource.AllInstances, includeField, false, 0, 0, filters, new HashSet<CustomTagFilterDetails>() { filterDetails1, filterDetails2, filterDetails3 });

            var parm = new SqlQueryParameterManager(CreateSqlParameterCollection());
            new SqlQueryGenerator(stringBuilder, query, parm);

            string expectedCustomTagTableFilter = @"INNER JOIN dbo.CustomTagString cts1
ON cts1.StudyKey = st.StudyKey
AND ON cts1.SeriesKey = se.SeriesKey
AND ON cts1.InstanceKey = i.InstanceKey
INNER JOIN dbo.CustomTagString cts2
ON cts2.StudyKey = st.StudyKey
AND ON cts2.SeriesKey = se.SeriesKey
INNER JOIN dbo.CustomTagBigInt ctbi4
ON ctbi4.StudyKey = st.StudyKey
WHERE";

            string expectedFilters = @"AND cts1.TagKey=@p0
AND cts1.TagValue=@p1
AND cts2.TagKey=@p2
AND cts2.TagValue=@p3
AND ctbi4.TagKey=@p4
AND ctbi4.TagValue=@p5";

            string builtString = stringBuilder.ToString();
            Assert.Contains(expectedCustomTagTableFilter, builtString);
            Assert.Contains(expectedFilters, builtString);
        }

        [Fact]
        public void GivenPatientNameFilter_WithFuzzyMatchMultiWord_ValidateContainsFilterGenerated()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(false, new List<DicomTag>());
            var filters = new List<QueryFilterCondition>()
            {
                new PersonNameFuzzyMatchCondition(DicomTag.PatientName, "Fall 6"),
            };
            var query = new QueryExpression(QueryResource.AllStudies, includeField, true, 10, 0, filters);
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
