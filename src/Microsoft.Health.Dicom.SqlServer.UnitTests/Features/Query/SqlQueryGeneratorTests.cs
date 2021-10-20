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
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.SqlServer.Features.Query;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Features.Storage;
using Xunit;

namespace Microsoft.Health.Dicom.SqlServer.UnitTests.Features.Query
{
    public class SqlQueryGeneratorTests
    {
        private const string SqlDateFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        [Fact]
        public void GivenStudyDate_WhenIELevelStudy_ValidateDistinctStudyStudies()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(new List<DicomTag>());
            var minDate = new DateTime(2020, 2, 1);
            var maxDate = new DateTime(2020, 3, 1);

            var filters = new List<QueryFilterCondition>()
            {
                new DateRangeValueMatchCondition(new QueryTag(DicomTag.StudyDate), minDate, maxDate),
            };
            var query = new QueryExpression(QueryResource.AllStudies, includeField, false, 0, 0, filters, Array.Empty<string>());

            var parm = new SqlQueryParameterManager(CreateSqlParameterCollection());
            new SqlQueryGenerator(stringBuilder, query, parm, SqlServer.Features.Schema.SchemaVersion.V6);

            string expectedDistinctSelect = @"SELECT 
st.StudyKey
FROM dbo.Study st
WHERE 1 = 1
AND st.PartitionKey = 1";

            string expectedCrossApply = @"
FROM dbo.Instance a
WHERE 1 = 1
AND a.StudyKey = f.StudyKey
AND a.Status = 1 
ORDER BY a.Watermark DESC";

            string expectedFilterAndPage = @"
AND st.StudyDate BETWEEN @p0 AND @p1
ORDER BY st.StudyKey DESC
OFFSET 0 ROWS
FETCH NEXT 100 ROWS ONLY";

            Assert.Contains(expectedDistinctSelect, stringBuilder.ToString());
            Assert.Contains(expectedCrossApply, stringBuilder.ToString());
            Assert.Contains(expectedFilterAndPage, stringBuilder.ToString());
        }

        [Fact]
        public void GivenModality_WhenIELevelSeries_ValidateDistinctSeries()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(new List<DicomTag>());
            var filters = new List<QueryFilterCondition>()
            {
                new StringSingleValueMatchCondition(new QueryTag(DicomTag.Modality), "123"),
            };
            var query = new QueryExpression(QueryResource.AllSeries, includeField, false, 0, 0, filters, Array.Empty<string>());

            var parm = new SqlQueryParameterManager(CreateSqlParameterCollection());
            new SqlQueryGenerator(stringBuilder, query, parm, SqlServer.Features.Schema.SchemaVersion.V4);

            string expectedDistinctSelect = @"SELECT 
st.StudyKey
,se.SeriesKey
FROM dbo.Study st
INNER JOIN dbo.Series se
ON se.StudyKey = st.StudyKey";

            string expectedFilterAndPage = @"
AND se.Modality=@p0
ORDER BY se.SeriesKey DESC
OFFSET 0 ROWS
FETCH NEXT 100 ROWS ONLY";

            string expectedCrossApply = @"
FROM dbo.Instance a
WHERE 1 = 1
AND a.StudyKey = f.StudyKey
AND a.SeriesKey = f.SeriesKey
AND a.Status = 1 
ORDER BY a.Watermark DESC";



            Assert.Contains(expectedDistinctSelect, stringBuilder.ToString());
            Assert.Contains(expectedCrossApply, stringBuilder.ToString());
            Assert.Contains(expectedFilterAndPage, stringBuilder.ToString());
        }

        [Fact]
        public void GivenNonUidFilter_WhenIELevelInstance_ValidateDistinctInstances()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(new List<DicomTag>());
            var filters = new List<QueryFilterCondition>()
            {
                new StringSingleValueMatchCondition(new QueryTag(DicomTag.Modality), "123"),
            };
            var query = new QueryExpression(QueryResource.AllInstances, includeField, false, 0, 0, filters, Array.Empty<string>());

            var parm = new SqlQueryParameterManager(CreateSqlParameterCollection());
            new SqlQueryGenerator(stringBuilder, query, parm, SqlServer.Features.Schema.SchemaVersion.V4);

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

            string expectedPage = @"ORDER BY i.Watermark DESC
OFFSET 0 ROWS
FETCH NEXT 100 ROWS ONLY";

            Assert.Contains(expectedDistinctSelect, stringBuilder.ToString());
            Assert.Contains(expectedFilters, stringBuilder.ToString());
            Assert.Contains(expectedPage, stringBuilder.ToString());
            Assert.DoesNotContain("CROSS APPLY", stringBuilder.ToString());
        }

        [Fact]
        public void GivenStringExtendedQueryTagFilter_WhenIELevelStudy_ValidateExtendedQueryTagFilter()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(new List<DicomTag>());
            var queryTag = new QueryTag(DicomTag.ModelGroupUID.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));
            var filter = new StringSingleValueMatchCondition(queryTag, "123");
            filter.QueryTag = queryTag;
            var filters = new List<QueryFilterCondition>()
            {
                filter,
            };
            var query = new QueryExpression(QueryResource.AllStudies, includeField, false, 0, 0, filters, Array.Empty<string>());

            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm, SqlServer.Features.Schema.SchemaVersion.V4);

            string expectedExtendedQueryTagTableFilter = @"INNER JOIN dbo.ExtendedQueryTagString cts1
ON cts1.StudyKey = st.StudyKey
WHERE";

            string expectedFilters = @"AND cts1.TagKey=@p0
AND cts1.TagValue=@p1";

            string builtString = stringBuilder.ToString();
            Assert.Equal(queryTag.ExtendedQueryTagStoreEntry.Key.ToString(), sqlParameterCollection[0].Value.ToString());
            Assert.Equal(filter.Value.ToString(), sqlParameterCollection[1].Value.ToString());
            Assert.Contains(expectedExtendedQueryTagTableFilter, builtString);
            Assert.Contains(expectedFilters, builtString);
        }

        [Fact]
        public void GivenLongExtendedQueryTagFilter_WhenIELevelStudy_ValidateExtendedQueryTagFilter()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(new List<DicomTag>());
            var queryTag = new QueryTag(DicomTag.NumberOfAssessmentObservations.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));
            var filter = new LongSingleValueMatchCondition(queryTag, 123);
            filter.QueryTag = queryTag;
            var filters = new List<QueryFilterCondition>()
            {
                filter,
            };
            var query = new QueryExpression(QueryResource.AllStudies, includeField, false, 0, 0, filters, Array.Empty<string>());

            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm, SqlServer.Features.Schema.SchemaVersion.V4);

            string expectedExtendedQueryTagTableFilter = @"INNER JOIN dbo.ExtendedQueryTagLong ctl1
ON ctl1.StudyKey = st.StudyKey
WHERE";

            string expectedFilters = @"AND ctl1.TagKey=@p0
AND ctl1.TagValue=@p1";

            string builtString = stringBuilder.ToString();
            Assert.Equal(queryTag.ExtendedQueryTagStoreEntry.Key.ToString(), sqlParameterCollection[0].Value.ToString());
            Assert.Equal(filter.Value.ToString(), sqlParameterCollection[1].Value.ToString());
            Assert.Contains(expectedExtendedQueryTagTableFilter, builtString);
            Assert.Contains(expectedFilters, builtString);
        }

        [Fact]
        public void GivenDoubleExtendedQueryTagFilter_WhenIELevelStudy_ValidateExtendedQueryTagFilter()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(new List<DicomTag>());
            var queryTag = new QueryTag(DicomTag.FloatingPointValue.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));
            var filter = new DoubleSingleValueMatchCondition(queryTag, 123D);
            filter.QueryTag = queryTag;
            var filters = new List<QueryFilterCondition>()
            {
                filter,
            };
            var query = new QueryExpression(QueryResource.AllStudies, includeField, false, 0, 0, filters, Array.Empty<string>());

            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm, SqlServer.Features.Schema.SchemaVersion.V4);

            string expectedExtendedQueryTagTableFilter = @"INNER JOIN dbo.ExtendedQueryTagDouble ctd1
ON ctd1.StudyKey = st.StudyKey
WHERE";

            string expectedFilters = @"AND ctd1.TagKey=@p0
AND ctd1.TagValue=@p1";

            string builtString = stringBuilder.ToString();
            Assert.Equal(queryTag.ExtendedQueryTagStoreEntry.Key.ToString(), sqlParameterCollection[0].Value.ToString());
            Assert.Equal(filter.Value.ToString(), sqlParameterCollection[1].Value.ToString());
            Assert.Contains(expectedExtendedQueryTagTableFilter, builtString);
            Assert.Contains(expectedFilters, builtString);
        }

        [Fact]
        public void GivenDateExtendedQueryTagFilter_WhenIELevelStudy_ValidateExtendedQueryTagFilter()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(new List<DicomTag>());
            var queryTag = new QueryTag(DicomTag.Date.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));
            var filter = new DateRangeValueMatchCondition(queryTag, DateTime.ParseExact("19510910", QueryParser.DateTagValueFormat, null), DateTime.ParseExact("19571110", QueryParser.DateTagValueFormat, null));

            filter.QueryTag = queryTag;
            var filters = new List<QueryFilterCondition>()
            {
                filter,
            };
            var query = new QueryExpression(QueryResource.AllStudies, includeField, false, 0, 0, filters, Array.Empty<string>());

            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm, SqlServer.Features.Schema.SchemaVersion.V4);

            string expectedExtendedQueryTagTableFilter = @"INNER JOIN dbo.ExtendedQueryTagDateTime ctdt1
ON ctdt1.StudyKey = st.StudyKey
WHERE";

            string expectedFilters = @"AND ctdt1.TagKey=@p0
AND ctdt1.TagValue BETWEEN @p1 AND @p2";

            string builtString = stringBuilder.ToString();
            Assert.Equal(queryTag.ExtendedQueryTagStoreEntry.Key.ToString(), sqlParameterCollection[0].Value.ToString());
            Assert.Equal(filter.Minimum.ToString(SqlDateFormat), sqlParameterCollection[1].Value.ToString());
            Assert.Equal(filter.Maximum.ToString(SqlDateFormat), sqlParameterCollection[2].Value.ToString());
            Assert.Contains(expectedExtendedQueryTagTableFilter, builtString);
            Assert.Contains(expectedFilters, builtString);
        }

        [Fact]
        public void GivenDateTimeExtendedQueryTagFilter_WhenIELevelStudy_ValidateExtendedQueryTagFilter()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(new List<DicomTag>());
            var queryTag = new QueryTag(DicomTag.DateTime.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));
            var filter = new DateRangeValueMatchCondition(queryTag, DateTime.ParseExact("19510910111213.123", QueryParser.DateTimeTagValueFormats, null), DateTime.ParseExact("19571110111213.123", QueryParser.DateTimeTagValueFormats, null));

            filter.QueryTag = queryTag;
            var filters = new List<QueryFilterCondition>()
            {
                filter,
            };
            var query = new QueryExpression(QueryResource.AllStudies, includeField, false, 0, 0, filters, Array.Empty<string>());

            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm, SqlServer.Features.Schema.SchemaVersion.V4);

            string expectedExtendedQueryTagTableFilter = @"INNER JOIN dbo.ExtendedQueryTagDateTime ctdt1
ON ctdt1.StudyKey = st.StudyKey
WHERE";

            string expectedFilters = @"AND ctdt1.TagKey=@p0
AND ctdt1.TagValue BETWEEN @p1 AND @p2";

            string builtString = stringBuilder.ToString();
            Assert.Equal(queryTag.ExtendedQueryTagStoreEntry.Key.ToString(), sqlParameterCollection[0].Value.ToString());
            Assert.Equal(filter.Minimum.ToString(SqlDateFormat), sqlParameterCollection[1].Value.ToString());
            Assert.Equal(filter.Maximum.ToString(SqlDateFormat), sqlParameterCollection[2].Value.ToString());
            Assert.Contains(expectedExtendedQueryTagTableFilter, builtString);
            Assert.Contains(expectedFilters, builtString);
        }

        [Fact]
        public void GivenTimeExtendedQueryTagFilter_WhenIELevelStudy_ValidateExtendedQueryTagFilter()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(new List<DicomTag>());
            var queryTag = new QueryTag(DicomTag.Time.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));

            long minTicks = new DicomTime(queryTag.Tag, new string[] { "111213.123" }).Get<DateTime>().Ticks;
            long maxTicks = new DicomTime(queryTag.Tag, new string[] { "111214.123" }).Get<DateTime>().Ticks;
            var filter = new LongRangeValueMatchCondition(queryTag, minTicks, maxTicks);

            filter.QueryTag = queryTag;
            var filters = new List<QueryFilterCondition>()
            {
                filter,
            };
            var query = new QueryExpression(QueryResource.AllStudies, includeField, false, 0, 0, filters, Array.Empty<string>());

            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm, SqlServer.Features.Schema.SchemaVersion.V4);

            string expectedExtendedQueryTagTableFilter = @"INNER JOIN dbo.ExtendedQueryTagLong ctl1
ON ctl1.StudyKey = st.StudyKey
WHERE";

            string expectedFilters = @"AND ctl1.TagKey=@p0
AND ctl1.TagValue BETWEEN @p1 AND @p2";

            string builtString = stringBuilder.ToString();
            Assert.Equal(queryTag.ExtendedQueryTagStoreEntry.Key.ToString(), sqlParameterCollection[0].Value.ToString());
            Assert.Equal(filter.Minimum.ToString(), sqlParameterCollection[1].Value.ToString());
            Assert.Equal(filter.Maximum.ToString(), sqlParameterCollection[2].Value.ToString());
            Assert.Contains(expectedExtendedQueryTagTableFilter, builtString);
            Assert.Contains(expectedFilters, builtString);
        }

        [Fact]
        public void GivenExtendedQueryTagFilterWithNonUidFilter_WhenIELevelSeries_ValidateExtendedQueryTagFilter()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(new List<DicomTag>());
            var queryTag = new QueryTag(DicomTag.ModelGroupUID.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));
            var extendedQueryTagFilter = new StringSingleValueMatchCondition(queryTag, "123");
            extendedQueryTagFilter.QueryTag = queryTag;
            var filter = new StringSingleValueMatchCondition(new QueryTag(DicomTag.Modality), "abc");
            var filters = new List<QueryFilterCondition>()
            {
                filter,
                extendedQueryTagFilter,
            };
            var query = new QueryExpression(QueryResource.StudySeries, includeField, false, 0, 0, filters, Array.Empty<string>());

            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm, SqlServer.Features.Schema.SchemaVersion.V4);

            string expectedExtendedQueryTagTableFilter = @"INNER JOIN dbo.ExtendedQueryTagString cts1
ON cts1.StudyKey = st.StudyKey
AND cts1.SeriesKey = se.SeriesKey
WHERE";

            string expectedFilter = @"AND se.Modality=@p0";
            string expectedExtendedQueryTagFilter = @"AND cts1.TagKey=@p1
AND cts1.TagValue=@p2";

            string builtString = stringBuilder.ToString();
            Assert.Equal(queryTag.ExtendedQueryTagStoreEntry.Key.ToString(), sqlParameterCollection[1].Value.ToString());
            Assert.Equal(extendedQueryTagFilter.Value.ToString(), sqlParameterCollection[2].Value.ToString());
            Assert.Contains(expectedExtendedQueryTagTableFilter, builtString);
            Assert.Contains(expectedFilter, builtString);
            Assert.Contains(expectedExtendedQueryTagFilter, builtString);
        }

        [Fact]
        public void GivenMultipleExtendedQueryTagFiltersOnSameLevel_WhenIELevelInstance_ValidateExtendedQueryTagFilter()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(new List<DicomTag>());
            var queryTag1 = new QueryTag(DicomTag.ModelGroupUID.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));
            var filter1 = new StringSingleValueMatchCondition(queryTag1, "abc");
            filter1.QueryTag = queryTag1;
            var queryTag2 = new QueryTag(DicomTag.ContainerDescription.BuildExtendedQueryTagStoreEntry(key: 2, level: QueryTagLevel.Series));
            var filter2 = new StringSingleValueMatchCondition(queryTag2, "description");
            filter2.QueryTag = queryTag2;
            var filters = new List<QueryFilterCondition>()
            {
                filter1,
                filter2,
            };
            var query = new QueryExpression(QueryResource.AllInstances, includeField, false, 0, 0, filters, Array.Empty<string>());

            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm, SqlServer.Features.Schema.SchemaVersion.V4);

            // cts1 is associated with filter1 which is at the instance level. This means the join should be on all three keys.
            // cts2 is associated with filter2 which is at the series level. This means the join should be on only study and series keys.
            // ctl4 is associated with filter3 which is at the study level. This means the join should be on only the study key.
            string expectedExtendedQueryTagTableFilter = @"INNER JOIN dbo.ExtendedQueryTagString cts1
ON cts1.StudyKey = st.StudyKey
AND cts1.SeriesKey = se.SeriesKey
INNER JOIN dbo.ExtendedQueryTagString cts2
ON cts2.StudyKey = st.StudyKey
AND cts2.SeriesKey = se.SeriesKey
WHERE";

            string expectedFilters = @"AND cts1.TagKey=@p0
AND cts1.TagValue=@p1
AND cts2.TagKey=@p2
AND cts2.TagValue=@p3";

            string builtString = stringBuilder.ToString();
            Assert.Equal(queryTag1.ExtendedQueryTagStoreEntry.Key.ToString(), sqlParameterCollection[0].Value.ToString());
            Assert.Equal(filter1.Value.ToString(), sqlParameterCollection[1].Value.ToString());
            Assert.Equal(queryTag2.ExtendedQueryTagStoreEntry.Key.ToString(), sqlParameterCollection[2].Value.ToString());
            Assert.Equal(filter2.Value.ToString(), sqlParameterCollection[3].Value.ToString());
            Assert.Contains(expectedExtendedQueryTagTableFilter, builtString);
            Assert.Contains(expectedFilters, builtString);
        }

        [Fact]
        public void GivenMultipleExtendedQueryTagFiltersOnDifferentLevels_WhenIELevelInstance_ValidateExtendedQueryTagFilter()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(new List<DicomTag>());
            var queryTag1 = new QueryTag(DicomTag.ModelGroupUID.BuildExtendedQueryTagStoreEntry(key: 1, level: QueryTagLevel.Instance));
            var filter1 = new StringSingleValueMatchCondition(queryTag1, "abc");
            filter1.QueryTag = queryTag1;

            var queryTag2 = new QueryTag(DicomTag.ContainerDescription.BuildExtendedQueryTagStoreEntry(key: 2, level: QueryTagLevel.Series));
            var filter2 = new StringSingleValueMatchCondition(queryTag2, "description");
            filter2.QueryTag = queryTag2;

            var queryTag3 = new QueryTag(DicomTag.NumberOfAssessmentObservations.BuildExtendedQueryTagStoreEntry(key: 4, level: QueryTagLevel.Study));
            var filter3 = new LongSingleValueMatchCondition(queryTag3, 123);
            filter3.QueryTag = queryTag3;
            var filters = new List<QueryFilterCondition>()
            {
                filter1,
                filter2,
                filter3,
            };
            var query = new QueryExpression(QueryResource.AllInstances, includeField, false, 0, 0, filters, Array.Empty<string>());

            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm, SqlServer.Features.Schema.SchemaVersion.V4);

            // cts1 is associated with filter1 which is at the instance level. This means the join should be on all three keys.
            // cts2 is associated with filter2 which is at the series level. This means the join should be on only study and series keys.
            // ctl4 is associated with filter3 which is at the study level. This means the join should be on only the study key.
            string expectedExtendedQueryTagTableFilter = @"INNER JOIN dbo.ExtendedQueryTagString cts1
ON cts1.StudyKey = st.StudyKey
AND cts1.SeriesKey = se.SeriesKey
AND cts1.InstanceKey = i.InstanceKey
INNER JOIN dbo.ExtendedQueryTagString cts2
ON cts2.StudyKey = st.StudyKey
AND cts2.SeriesKey = se.SeriesKey
INNER JOIN dbo.ExtendedQueryTagLong ctl4
ON ctl4.StudyKey = st.StudyKey
WHERE";

            string expectedFilters = @"AND cts1.TagKey=@p0
AND cts1.TagValue=@p1
AND cts2.TagKey=@p2
AND cts2.TagValue=@p3
AND ctl4.TagKey=@p4
AND ctl4.TagValue=@p5";

            string builtString = stringBuilder.ToString();
            Assert.Equal(queryTag1.ExtendedQueryTagStoreEntry.Key.ToString(), sqlParameterCollection[0].Value.ToString());
            Assert.Equal(filter1.Value.ToString(), sqlParameterCollection[1].Value.ToString());
            Assert.Equal(queryTag2.ExtendedQueryTagStoreEntry.Key.ToString(), sqlParameterCollection[2].Value.ToString());
            Assert.Equal(filter2.Value.ToString(), sqlParameterCollection[3].Value.ToString());
            Assert.Equal(queryTag3.ExtendedQueryTagStoreEntry.Key.ToString(), sqlParameterCollection[4].Value.ToString());
            Assert.Equal(filter3.Value.ToString(), sqlParameterCollection[5].Value.ToString());
            Assert.Contains(expectedExtendedQueryTagTableFilter, builtString);
            Assert.Contains(expectedFilters, builtString);
        }

        [Fact]
        public void GivenPatientNameFilter_WithFuzzyMatchMultiWord_ValidateContainsFilterGenerated()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(new List<DicomTag>());
            var filters = new List<QueryFilterCondition>()
            {
                new PersonNameFuzzyMatchCondition(new QueryTag(DicomTag.PatientName), "Fall 6"),
            };
            var query = new QueryExpression(QueryResource.AllStudies, includeField, true, 10, 0, filters, Array.Empty<string>());
            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm, SqlServer.Features.Schema.SchemaVersion.V4);

            string expectedParam = $"\"Fall 6*\"";

            string expectedFilters = @"AND CONTAINS(st.PatientNameWords, @p0)";

            Assert.Equal(expectedParam, sqlParameterCollection[0].Value.ToString());
            Assert.Contains(expectedFilters, stringBuilder.ToString());
        }

        [Fact]
        public void GivenPatientNameFilterForExtendedQueryTag_WithFuzzyMatchMultiWord_ValidateContainsFilterGenerated()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new QueryIncludeField(new List<DicomTag>());
            var queryTag = new QueryTag(DicomTag.ConsultingPhysicianName.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));
            var filter = new PersonNameFuzzyMatchCondition(queryTag, "Fall 6");
            filter.QueryTag = queryTag;
            var filters = new List<QueryFilterCondition>()
            {
                filter,
            };
            var query = new QueryExpression(QueryResource.AllInstances, includeField, true, 10, 0, filters, Array.Empty<string>());
            SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
            var parm = new SqlQueryParameterManager(sqlParameterCollection);
            new SqlQueryGenerator(stringBuilder, query, parm, SqlServer.Features.Schema.SchemaVersion.V4);

            string expectedParam = $"\"Fall 6*\"";

            string expectedFilters = @"AND ctpn1.TagKey=@p0
AND CONTAINS(ctpn1.TagValueWords, @p1)";

            Assert.Equal(queryTag.ExtendedQueryTagStoreEntry.Key.ToString(), sqlParameterCollection[0].Value.ToString());
            Assert.Equal(expectedParam, sqlParameterCollection[1].Value.ToString());
            Assert.Contains(expectedFilters, stringBuilder.ToString());
        }

        private SqlParameterCollection CreateSqlParameterCollection()
        {
            return (SqlParameterCollection)typeof(SqlParameterCollection).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null).Invoke(null);
        }
    }
}
