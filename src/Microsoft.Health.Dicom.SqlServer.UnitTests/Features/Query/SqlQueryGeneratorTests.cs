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
using Microsoft.Health.Dicom.Core.Extensions;
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
        public void GivenStringCustomTagFilter_WhenIELevelStudy_ValidateCustomTagFilter()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(false, new List<DicomTag>());
            var filter = new StringSingleValueMatchCondition(DicomTag.ModelGroupUID, "123");
            var filterDetails = new CustomTagFilterDetails(1, CustomTagLevel.Study, DicomTag.ModelGroupUID.GetDefaultVR(), DicomTag.ModelGroupUID);
            filter.CustomTagFilterDetails = filterDetails;
            var filters = new List<QueryFilterCondition>()
            {
                filter,
            };
            var query = new QueryExpression(QueryResource.AllStudies, includeField, false, 0, 0, filters, new List<CustomTagFilterDetails>() { filterDetails });

            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm);

            string expectedCustomTagTableFilter = @"INNER JOIN dbo.CustomTagString cts1
ON cts1.StudyKey = st.StudyKey
WHERE";

            string expectedFilters = @"AND cts1.TagKey=@p0
AND cts1.TagValue=@p1";

            string builtString = stringBuilder.ToString();
            Assert.Equal(filterDetails.Key.ToString(), sqlParameterCollection[0].Value.ToString());
            Assert.Equal(filter.Value.ToString(), sqlParameterCollection[1].Value.ToString());
            Assert.Contains(expectedCustomTagTableFilter, builtString);
            Assert.Contains(expectedFilters, builtString);
        }

        [Fact]
        public void GivenLongCustomTagFilter_WhenIELevelStudy_ValidateCustomTagFilter()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(false, new List<DicomTag>());
            var filter = new LongSingleValueMatchCondition(DicomTag.NumberOfAssessmentObservations, 123);
            var filterDetails = new CustomTagFilterDetails(1, CustomTagLevel.Study, DicomTag.NumberOfAssessmentObservations.GetDefaultVR(), DicomTag.NumberOfAssessmentObservations);
            filter.CustomTagFilterDetails = filterDetails;
            var filters = new List<QueryFilterCondition>()
            {
                filter,
            };
            var query = new QueryExpression(QueryResource.AllStudies, includeField, false, 0, 0, filters, new List<CustomTagFilterDetails>() { filterDetails });

            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm);

            string expectedCustomTagTableFilter = @"INNER JOIN dbo.CustomTagBigInt ctbi1
ON ctbi1.StudyKey = st.StudyKey
WHERE";

            string expectedFilters = @"AND ctbi1.TagKey=@p0
AND ctbi1.TagValue=@p1";

            string builtString = stringBuilder.ToString();
            Assert.Equal(filterDetails.Key.ToString(), sqlParameterCollection[0].Value.ToString());
            Assert.Equal(filter.Value.ToString(), sqlParameterCollection[1].Value.ToString());
            Assert.Contains(expectedCustomTagTableFilter, builtString);
            Assert.Contains(expectedFilters, builtString);
        }

        [Fact]
        public void GivenDoubleCustomTagFilter_WhenIELevelStudy_ValidateCustomTagFilter()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(false, new List<DicomTag>());
            var filter = new DoubleSingleValueMatchCondition(DicomTag.FloatingPointValue, 123D);
            var filterDetails = new CustomTagFilterDetails(1, CustomTagLevel.Study, DicomTag.FloatingPointValue.GetDefaultVR(), DicomTag.FloatingPointValue);
            filter.CustomTagFilterDetails = filterDetails;
            var filters = new List<QueryFilterCondition>()
            {
                filter,
            };
            var query = new QueryExpression(QueryResource.AllStudies, includeField, false, 0, 0, filters, new List<CustomTagFilterDetails>() { filterDetails });

            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm);

            string expectedCustomTagTableFilter = @"INNER JOIN dbo.CustomTagDouble ctd1
ON ctd1.StudyKey = st.StudyKey
WHERE";

            string expectedFilters = @"AND ctd1.TagKey=@p0
AND ctd1.TagValue=@p1";

            string builtString = stringBuilder.ToString();
            Assert.Equal(filterDetails.Key.ToString(), sqlParameterCollection[0].Value.ToString());
            Assert.Equal(filter.Value.ToString(), sqlParameterCollection[1].Value.ToString());
            Assert.Contains(expectedCustomTagTableFilter, builtString);
            Assert.Contains(expectedFilters, builtString);
        }

        [Fact]
        public void GivenDateCustomTagFilter_WhenIELevelStudy_ValidateCustomTagFilter()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(false, new List<DicomTag>());
            var filter = new DateRangeValueMatchCondition(DicomTag.Date, DateTime.ParseExact("19510910", QueryParser.DateTagValueFormat, null), DateTime.ParseExact("19571110", QueryParser.DateTagValueFormat, null));
            var filterDetails = new CustomTagFilterDetails(1, CustomTagLevel.Study, DicomTag.Date.GetDefaultVR(), DicomTag.Date);
            filter.CustomTagFilterDetails = filterDetails;
            var filters = new List<QueryFilterCondition>()
            {
                filter,
            };
            var query = new QueryExpression(QueryResource.AllStudies, includeField, false, 0, 0, filters, new List<CustomTagFilterDetails>() { filterDetails });

            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm);

            string expectedCustomTagTableFilter = @"INNER JOIN dbo.CustomTagDateTime ctdt1
ON ctdt1.StudyKey = st.StudyKey
WHERE";

            string expectedFilters = @"AND ctdt1.TagKey=@p0
AND ctdt1.TagValue BETWEEN @p1 AND @p2";

            string builtString = stringBuilder.ToString();
            Assert.Equal(filterDetails.Key.ToString(), sqlParameterCollection[0].Value.ToString());
            Assert.Equal(filter.Minimum.ToString("yyyy-MM-dd"), sqlParameterCollection[1].Value.ToString());
            Assert.Equal(filter.Maximum.ToString("yyyy-MM-dd"), sqlParameterCollection[2].Value.ToString());
            Assert.Contains(expectedCustomTagTableFilter, builtString);
            Assert.Contains(expectedFilters, builtString);
        }

        [Fact]
        public void GivenCustomTagFilterWithNonUidFilter_WhenIELevelSeries_ValidateCustomTagFilter()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(false, new List<DicomTag>());
            var customTagFilter = new StringSingleValueMatchCondition(DicomTag.ModelGroupUID, "123");
            var filterDetails = new CustomTagFilterDetails(1, CustomTagLevel.Series, DicomTag.ModelGroupUID.GetDefaultVR(), DicomTag.ModelGroupUID);
            customTagFilter.CustomTagFilterDetails = filterDetails;
            var filter = new StringSingleValueMatchCondition(DicomTag.Modality, "abc");
            var filters = new List<QueryFilterCondition>()
            {
                filter,
                customTagFilter,
            };
            var query = new QueryExpression(QueryResource.StudySeries, includeField, false, 0, 0, filters, new List<CustomTagFilterDetails>() { filterDetails });

            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm);

            string expectedCustomTagTableFilter = @"INNER JOIN dbo.CustomTagString cts1
ON cts1.StudyKey = st.StudyKey
AND ON cts1.SeriesKey = se.SeriesKey
WHERE";

            string expectedFilter = @"AND se.Modality=@p0";
            string expectedCustomTagFilter = @"AND cts1.TagKey=@p1
AND cts1.TagValue=@p2";

            string builtString = stringBuilder.ToString();
            Assert.Equal(filterDetails.Key.ToString(), sqlParameterCollection[1].Value.ToString());
            Assert.Equal(customTagFilter.Value.ToString(), sqlParameterCollection[2].Value.ToString());
            Assert.Contains(expectedCustomTagTableFilter, builtString);
            Assert.Contains(expectedFilter, builtString);
            Assert.Contains(expectedCustomTagFilter, builtString);
        }

        [Fact]
        public void GivenMultipleCustomTagFiltersOnSameLevel_WhenIELevelInstance_ValidateCustomTagFilter()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(false, new List<DicomTag>());
            var filter1 = new StringSingleValueMatchCondition(DicomTag.ModelGroupUID, "abc");
            var filterDetails1 = new CustomTagFilterDetails(1, CustomTagLevel.Series, DicomTag.ModelGroupUID.GetDefaultVR(), DicomTag.ModelGroupUID);
            filter1.CustomTagFilterDetails = filterDetails1;
            var filter2 = new StringSingleValueMatchCondition(DicomTag.ContainerDescription, "description");
            var filterDetails2 = new CustomTagFilterDetails(2, CustomTagLevel.Series, DicomTag.ContainerDescription.GetDefaultVR(), DicomTag.ContainerDescription);
            filter2.CustomTagFilterDetails = filterDetails2;
            var filters = new List<QueryFilterCondition>()
            {
                filter1,
                filter2,
            };
            var query = new QueryExpression(QueryResource.AllInstances, includeField, false, 0, 0, filters, new List<CustomTagFilterDetails>() { filterDetails1, filterDetails2 });

            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm);

            // cts1 is associated with filter1 which is at the instance level. This means the join should be on all three keys.
            // cts2 is associated with filter2 which is at the series level. This means the join should be on only study and series keys.
            // ctbi4 is associated with filter3 which is at the study level. This means the join should be on only the study key.
            string expectedCustomTagTableFilter = @"INNER JOIN dbo.CustomTagString cts1
ON cts1.StudyKey = st.StudyKey
AND ON cts1.SeriesKey = se.SeriesKey
INNER JOIN dbo.CustomTagString cts2
ON cts2.StudyKey = st.StudyKey
AND ON cts2.SeriesKey = se.SeriesKey
WHERE";

            string expectedFilters = @"AND cts1.TagKey=@p0
AND cts1.TagValue=@p1
AND cts2.TagKey=@p2
AND cts2.TagValue=@p3";

            string builtString = stringBuilder.ToString();
            Assert.Equal(filterDetails1.Key.ToString(), sqlParameterCollection[0].Value.ToString());
            Assert.Equal(filter1.Value.ToString(), sqlParameterCollection[1].Value.ToString());
            Assert.Equal(filterDetails2.Key.ToString(), sqlParameterCollection[2].Value.ToString());
            Assert.Equal(filter2.Value.ToString(), sqlParameterCollection[3].Value.ToString());
            Assert.Contains(expectedCustomTagTableFilter, builtString);
            Assert.Contains(expectedFilters, builtString);
        }

        [Fact]
        public void GivenMultipleCustomTagFiltersOnDifferentLevels_WhenIELevelInstance_ValidateCustomTagFilter()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(false, new List<DicomTag>());
            var filter1 = new StringSingleValueMatchCondition(DicomTag.ModelGroupUID, "abc");
            var filterDetails1 = new CustomTagFilterDetails(1, CustomTagLevel.Instance, DicomTag.ModelGroupUID.GetDefaultVR(), DicomTag.ModelGroupUID);
            filter1.CustomTagFilterDetails = filterDetails1;
            var filter2 = new StringSingleValueMatchCondition(DicomTag.ContainerDescription, "description");
            var filterDetails2 = new CustomTagFilterDetails(2, CustomTagLevel.Series, DicomTag.ContainerDescription.GetDefaultVR(), DicomTag.ContainerDescription);
            filter2.CustomTagFilterDetails = filterDetails2;
            var filter3 = new LongSingleValueMatchCondition(DicomTag.NumberOfAssessmentObservations, 123);
            var filterDetails3 = new CustomTagFilterDetails(4, CustomTagLevel.Study, DicomTag.NumberOfAssessmentObservations.GetDefaultVR(), DicomTag.NumberOfAssessmentObservations);
            filter3.CustomTagFilterDetails = filterDetails3;
            var filters = new List<QueryFilterCondition>()
            {
                filter1,
                filter2,
                filter3,
            };
            var query = new QueryExpression(QueryResource.AllInstances, includeField, false, 0, 0, filters, new List<CustomTagFilterDetails>() { filterDetails1, filterDetails2, filterDetails3 });

            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm);

            // cts1 is associated with filter1 which is at the instance level. This means the join should be on all three keys.
            // cts2 is associated with filter2 which is at the series level. This means the join should be on only study and series keys.
            // ctbi4 is associated with filter3 which is at the study level. This means the join should be on only the study key.
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
            Assert.Equal(filterDetails1.Key.ToString(), sqlParameterCollection[0].Value.ToString());
            Assert.Equal(filter1.Value.ToString(), sqlParameterCollection[1].Value.ToString());
            Assert.Equal(filterDetails2.Key.ToString(), sqlParameterCollection[2].Value.ToString());
            Assert.Equal(filter2.Value.ToString(), sqlParameterCollection[3].Value.ToString());
            Assert.Equal(filterDetails3.Key.ToString(), sqlParameterCollection[4].Value.ToString());
            Assert.Equal(filter3.Value.ToString(), sqlParameterCollection[5].Value.ToString());
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

            string expectedFilters = @"AND CONTAINS(st.PatientNameWords, @p0)";

            Assert.Equal(expectedParam, sqlParameterCollection[0].Value.ToString());
            Assert.Contains(expectedFilters, stringBuilder.ToString());
        }

        [Fact]
        public void GivenPatientNameFilterForCustomTag_WithFuzzyMatchMultiWord_ValidateContainsFilterGenerated()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(false, new List<DicomTag>());
            var filter = new PersonNameFuzzyMatchCondition(DicomTag.ConsultingPhysicianName, "Fall 6");
            var filterDetails = new CustomTagFilterDetails(1, CustomTagLevel.Series, DicomTag.ConsultingPhysicianName.GetDefaultVR(), DicomTag.ConsultingPhysicianName);
            filter.CustomTagFilterDetails = filterDetails;
            var filters = new List<QueryFilterCondition>()
            {
                filter,
            };
            var query = new QueryExpression(QueryResource.AllInstances, includeField, true, 10, 0, filters, new List<CustomTagFilterDetails>() { filterDetails });
            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm);

            string expectedParam = $"\"Fall*\" AND \"6*\"";

            string expectedFilters = @"AND ctpn1.TagKey=@p0
AND CONTAINS(ctpn1.TagValueWords, @p1)";

            Assert.Equal(filterDetails.Key.ToString(), sqlParameterCollection[0].Value.ToString());
            Assert.Equal(expectedParam, sqlParameterCollection[1].Value.ToString());
            Assert.Contains(expectedFilters, stringBuilder.ToString());
        }

        private SqlParameterCollection CreateSqlParameterCollection()
        {
            return (SqlParameterCollection)typeof(SqlParameterCollection).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null).Invoke(null);
        }
    }
}
