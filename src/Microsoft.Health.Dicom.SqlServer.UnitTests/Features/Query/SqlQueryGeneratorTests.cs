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
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Fhir.SqlServer;
using Microsoft.Health.Fhir.SqlServer.Features.Query;
using Microsoft.Health.Fhir.SqlServer.Features.Storage;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Query
{
    public class SqlQueryGeneratorTests
    {
        [Fact]
        public void GivenStudyInstanceUID_WhenIELevelSeries_ValidateDistinctStudySeries()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new DicomQueryParameterIncludeField(false, new List<DicomTag>());
            var filters = new List<DicomQueryFilterCondition>()
            {
                new StringSingleValueMatchCondition(DicomTag.StudyInstanceUID, "1234"),
            };
            var query = new DicomQueryExpression(QueryResource.StudySeries, includeField, false, 0, 0, filters);

            var parm = new SqlQueryParameterManager(CreateSqlParameterCollection());
            new SqlQueryGenerator(stringBuilder, query, parm);

            string expectedDistinctSelect = @"SELECT DISTINCT
i.StudyInstanceUID
,i.SeriesInstanceUID
FROM dicom.Instance i";
            string expectedCrossApply = @"SELECT TOP 1 *
FROM dicom.Instance a
WHERE 1 = 1
AND a.StudyInstanceUID = f.StudyInstanceUID
AND a.SeriesInstanceUID = f.SeriesInstanceUID";

            Assert.Contains(expectedDistinctSelect, stringBuilder.ToString());
            Assert.Contains(expectedCrossApply, stringBuilder.ToString());
            Assert.Contains("StudyInstanceUID=@p0", stringBuilder.ToString());
            Assert.Contains($"OFFSET 0 ROWS", stringBuilder.ToString());
            Assert.Contains($"FETCH NEXT {DicomQueryLimit.DefaultQueryResultCount} ROWS ONLY", stringBuilder.ToString());
        }

        [Fact]
        public void GivenStudyDate_WhenIELevelSTudy_ValidateDistinctStudyStudies()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new DicomQueryParameterIncludeField(false, new List<DicomTag>());
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
st.StudyInstanceUID
FROM dicom.StudyMetadataCore st";

            string expectedCrossApply = @"SELECT TOP 1 *
FROM dicom.Instance a
WHERE 1 = 1
AND a.StudyInstanceUID = f.StudyInstanceUID";
            Assert.Contains(expectedDistinctSelect, stringBuilder.ToString());
            Assert.Contains(expectedCrossApply, stringBuilder.ToString());

            Assert.Contains("StudyDate BETWEEN @p0 AND @p1", stringBuilder.ToString());
        }

        [Fact]
        public void GivenSOPInstanceUID_WhenIELevelInstance_ValidateDistinctInstances()
        {
            var stringBuilder = new IndentedStringBuilder(new StringBuilder());
            var includeField = new DicomQueryParameterIncludeField(false, new List<DicomTag>());
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
i.StudyInstanceUID
,i.SeriesInstanceUID
,i.SOPInstanceUID
,i.Watermark
FROM dicom.Instance i";

            string expectedFilters = @"AND i.StudyInstanceUID=@p0
AND i.SeriesInstanceUID=@p1
AND i.SOPInstanceUID=@p2";

            Assert.Contains(expectedDistinctSelect, stringBuilder.ToString());
            Assert.Contains(expectedFilters, stringBuilder.ToString());
            Assert.DoesNotContain("CROSS APPLY", stringBuilder.ToString());
        }

        private SqlParameterCollection CreateSqlParameterCollection()
        {
            return (SqlParameterCollection)typeof(SqlParameterCollection).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null).Invoke(null);
        }
    }
}
