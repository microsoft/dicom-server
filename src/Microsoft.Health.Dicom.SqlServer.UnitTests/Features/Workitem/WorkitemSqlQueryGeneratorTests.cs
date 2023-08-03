// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using FellowOakDicom;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.SqlServer.Features.Workitem;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Features.Storage;
using Xunit;

namespace Microsoft.Health.Dicom.SqlServer.UnitTests.Features.Query;

public class WorkitemSqlQueryGeneratorTests
{
    private const string SqlDateFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

    [Fact]
    public void GivenWorkitemQueryTagFilter_ValidateGeneratedSqlFilters()
    {
        var stringBuilder = new IndentedStringBuilder(new StringBuilder());
        var includeField = new QueryIncludeField(new List<DicomTag>());
        var item = new DicomAgeString(
                 DicomTag.PatientName, "Foo");
        QueryTag queryTag = new QueryTag(DicomTagExtensions.BuildWorkitemQueryTagStoreEntry("00100010", 1, item.ValueRepresentation.Code));
        var filter = new StringSingleValueMatchCondition(queryTag, "Foo");
        filter.QueryTag = queryTag;
        var filters = new List<QueryFilterCondition>()
        {
            filter,
        };
        var query = new BaseQueryExpression(includeField, false, 0, 0, filters);

        SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
        var parm = new SqlQueryParameterManager(sqlParameterCollection);
        new WorkitemSqlQueryGenerator(stringBuilder, query, parm, SqlServer.Features.Schema.SchemaVersion.V9, Partition.DefaultKey);

        string expectedExtendedQueryTagTableFilter = @"INNER JOIN dbo.ExtendedQueryTagString cts1
ON cts1.PartitionKey = w.PartitionKey
AND cts1.ResourceType = 1
AND cts1.SopInstanceKey1 = w.WorkitemKey
WHERE 1 = 1
AND w.Status = 1 ";

        string expectedFilters = @"AND cts1.TagKey=@p0
AND cts1.TagValue=@p1";

        string builtString = stringBuilder.ToString();
        Assert.Equal(queryTag.WorkitemQueryTagStoreEntry.Key.ToString(CultureInfo.InvariantCulture), sqlParameterCollection[0].Value.ToString());
        Assert.Equal(filter.Value, sqlParameterCollection[1].Value.ToString());
        Assert.Contains(expectedExtendedQueryTagTableFilter, builtString);
        Assert.Contains(expectedFilters, builtString);
    }

    [Fact]
    public void GivenDateWorkitemQueryTagFilter_ValidateExtendedQueryTagFilter()
    {
        var stringBuilder = new IndentedStringBuilder(new StringBuilder());
        var includeField = new QueryIncludeField(new List<DicomTag>());
        var queryTag = new QueryTag(DicomTagExtensions.BuildWorkitemQueryTagStoreEntry("00404005", 1, "DT"));
        var filter = new DateRangeValueMatchCondition(queryTag, DateTime.ParseExact("19510910", QueryParser.DateTagValueFormat, null), DateTime.ParseExact("19571110", QueryParser.DateTagValueFormat, null));

        filter.QueryTag = queryTag;
        var filters = new List<QueryFilterCondition>()
        {
            filter,
        };
        var query = new BaseQueryExpression(includeField, false, 0, 0, filters);

        SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
        var parm = new SqlQueryParameterManager(sqlParameterCollection);
        new WorkitemSqlQueryGenerator(stringBuilder, query, parm, SqlServer.Features.Schema.SchemaVersion.V9, Partition.DefaultKey);

        string expectedExtendedQueryTagTableFilter = @"INNER JOIN dbo.ExtendedQueryTagDateTime ctdt1
ON ctdt1.PartitionKey = w.PartitionKey
AND ctdt1.ResourceType = 1
AND ctdt1.SopInstanceKey1 = w.WorkitemKey
WHERE";

        string expectedFilters = @"AND ctdt1.TagKey=@p0
AND ctdt1.TagValue BETWEEN @p1 AND @p2";

        string builtString = stringBuilder.ToString();
        Assert.Equal(queryTag.WorkitemQueryTagStoreEntry.Key.ToString(CultureInfo.InvariantCulture), sqlParameterCollection[0].Value.ToString());
        Assert.Equal(filter.Minimum.ToString(SqlDateFormat, CultureInfo.InvariantCulture), sqlParameterCollection[1].Value.ToString());
        Assert.Equal(filter.Maximum.ToString(SqlDateFormat, CultureInfo.InvariantCulture), sqlParameterCollection[2].Value.ToString());
        Assert.Contains(expectedExtendedQueryTagTableFilter, builtString);
        Assert.Contains(expectedFilters, builtString);
    }

    [Fact]
    public void GivenMultipleWorkitemQueryTagFilters_ValidateExtendedQueryTagFilter()
    {
        var stringBuilder = new IndentedStringBuilder(new StringBuilder());
        var includeField = new QueryIncludeField(new List<DicomTag>());
        var queryTag1 = new QueryTag(DicomTagExtensions.BuildWorkitemQueryTagStoreEntry("00100020", 1, "LO"));
        var filter1 = new StringSingleValueMatchCondition(queryTag1, "abc");
        filter1.QueryTag = queryTag1;
        var queryTag2 = new QueryTag(DicomTagExtensions.BuildWorkitemQueryTagStoreEntry("00404026.00080100", 2, "SH"));
        var filter2 = new StringSingleValueMatchCondition(queryTag2, "description");
        filter2.QueryTag = queryTag2;
        var filters = new List<QueryFilterCondition>()
        {
            filter1,
            filter2,
        };
        var query = new BaseQueryExpression(includeField, false, 0, 0, filters);

        SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
        var parm = new SqlQueryParameterManager(sqlParameterCollection);
        new WorkitemSqlQueryGenerator(stringBuilder, query, parm, SqlServer.Features.Schema.SchemaVersion.V9, Partition.DefaultKey);

        string expectedExtendedQueryTagTableFilter = @"INNER JOIN dbo.ExtendedQueryTagString cts1
ON cts1.PartitionKey = w.PartitionKey
AND cts1.ResourceType = 1
AND cts1.SopInstanceKey1 = w.WorkitemKey
INNER JOIN dbo.ExtendedQueryTagString cts2
ON cts2.PartitionKey = w.PartitionKey
AND cts2.ResourceType = 1
AND cts2.SopInstanceKey1 = w.WorkitemKey
WHERE 1 = 1
AND w.Status = 1 ";

        string expectedFilters = @"AND cts1.TagKey=@p0
AND cts1.TagValue=@p1
AND cts2.TagKey=@p2
AND cts2.TagValue=@p3";

        string builtString = stringBuilder.ToString();
        Assert.Equal(queryTag1.WorkitemQueryTagStoreEntry.Key.ToString(CultureInfo.InvariantCulture), sqlParameterCollection[0].Value.ToString());
        Assert.Equal(filter1.Value, sqlParameterCollection[1].Value.ToString());
        Assert.Equal(queryTag2.WorkitemQueryTagStoreEntry.Key.ToString(CultureInfo.InvariantCulture), sqlParameterCollection[2].Value.ToString());
        Assert.Equal(filter2.Value, sqlParameterCollection[3].Value.ToString());
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
            new PersonNameFuzzyMatchCondition(
                new QueryTag(
                    DicomTagExtensions.BuildWorkitemQueryTagStoreEntry("00100010", 1, "PN")),
                "Fall 6"),
        };
        var query = new BaseQueryExpression(includeField, true, 10, 0, filters);
        SqlParameterCollection sqlParameterCollection = CreateSqlParameterCollection();
        var parm = new SqlQueryParameterManager(sqlParameterCollection);
        new WorkitemSqlQueryGenerator(stringBuilder, query, parm, SqlServer.Features.Schema.SchemaVersion.V9, Partition.DefaultKey);

        string expectedParam = $"\"Fall 6*\"";

        string expectedFilters = @"AND CONTAINS(ctpn1.TagValueWords, @p1)";

        Assert.Equal(expectedParam, sqlParameterCollection[1].Value.ToString());
        Assert.Contains(expectedFilters, stringBuilder.ToString());
    }

    private static SqlParameterCollection CreateSqlParameterCollection()
    {
        return (SqlParameterCollection)typeof(SqlParameterCollection).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null).Invoke(null);
    }
}
