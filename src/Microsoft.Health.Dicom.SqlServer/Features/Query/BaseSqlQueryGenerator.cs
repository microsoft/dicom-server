// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Query;

internal abstract class BaseSqlQueryGenerator : QueryFilterConditionVisitor
{
    private readonly SqlQueryParameterManager _parameters;

    private const string SqlDateFormat = "yyyy-MM-dd HH:mm:ss.ffffff";
    private const string InstanceTableAlias = "i";
    private const string StudyTableAlias = "st";
    private const string SeriesTableAlias = "se";

    protected const string ExtendedQueryTagLongTableAlias = "ctl";
    protected const string ExtendedQueryTagDateTimeTableAlias = "ctdt";
    protected const string ExtendedQueryTagDoubleTableAlias = "ctd";
    protected const string ExtendedQueryTagPersonNameTableAlias = "ctpn";
    protected const string ExtendedQueryTagStringTableAlias = "cts";

    protected BaseSqlQueryGenerator(
        IndentedStringBuilder stringBuilder,
        BaseQueryExpression queryExpression,
        SqlQueryParameterManager sqlQueryParameterManager,
        SchemaVersion schemaVersion,
        int partitionKey)
    {
        StringBuilder = stringBuilder;
        QueryExpression = queryExpression;
        _parameters = sqlQueryParameterManager;
        SchemaVersion = schemaVersion;
        PartitionKey = partitionKey;
    }

    protected IndentedStringBuilder StringBuilder { get; }

    protected BaseQueryExpression QueryExpression { get; }

    protected SchemaVersion SchemaVersion { get; }

    protected int PartitionKey { get; }

    protected abstract int? GetKeyFromQueryTag(QueryTag queryTag);

    protected abstract bool IsIndexedQueryTag(QueryTag queryTag);

    protected void AppendOptionRecompile()
    {
        StringBuilder.AppendLine("OPTION(RECOMPILE)");
    }

    protected void AppendLongSchemaQueryTables(QueryFilterCondition condition, out string extendedQueryTagTableAlias)
    {
        QueryTag queryTag = condition.QueryTag;
        int tagKey = GetKeyFromQueryTag(queryTag).Value;
        ExtendedQueryTagDataType dataType = ExtendedQueryTagLimit.ExtendedQueryTagVRAndDataTypeMapping[queryTag.VR.Code];
        StringBuilder.Append("INNER JOIN ");
        extendedQueryTagTableAlias = null;
        switch (dataType)
        {
            case ExtendedQueryTagDataType.StringData:
                extendedQueryTagTableAlias = ExtendedQueryTagStringTableAlias + tagKey;
                StringBuilder.AppendLine($"{VLatest.ExtendedQueryTagString.TableName} {extendedQueryTagTableAlias}");

                break;
            case ExtendedQueryTagDataType.LongData:
                extendedQueryTagTableAlias = ExtendedQueryTagLongTableAlias + tagKey;
                StringBuilder.AppendLine($"{VLatest.ExtendedQueryTagLong.TableName} {extendedQueryTagTableAlias}");

                break;
            case ExtendedQueryTagDataType.DoubleData:
                extendedQueryTagTableAlias = ExtendedQueryTagDoubleTableAlias + tagKey;
                StringBuilder.AppendLine($"{VLatest.ExtendedQueryTagDouble.TableName} {extendedQueryTagTableAlias}");

                break;
            case ExtendedQueryTagDataType.DateTimeData:
                extendedQueryTagTableAlias = ExtendedQueryTagDateTimeTableAlias + tagKey;
                StringBuilder.AppendLine($"{VLatest.ExtendedQueryTagDateTime.TableName} {extendedQueryTagTableAlias}");

                break;
            case ExtendedQueryTagDataType.PersonNameData:
                extendedQueryTagTableAlias = ExtendedQueryTagPersonNameTableAlias + tagKey;
                StringBuilder.AppendLine($"{VLatest.ExtendedQueryTagPersonName.TableName} {extendedQueryTagTableAlias}");

                break;
        }
    }

    public override void Visit(StringSingleValueMatchCondition stringSingleValueMatchCondition)
    {
        var queryTag = stringSingleValueMatchCondition.QueryTag;
        var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag, IsIndexedQueryTag(queryTag));

        var tableAlias = GetTableAlias(dicomTagSqlEntry, GetKeyFromQueryTag(queryTag));
        StringBuilder
            .Append("AND ");

        AppendExtendedQueryTagKeyFilter(dicomTagSqlEntry, tableAlias, stringSingleValueMatchCondition);

        StringBuilder
            .Append(dicomTagSqlEntry.SqlColumn, tableAlias)
            .Append("=")
            .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, stringSingleValueMatchCondition.Value))
            .AppendLine();
    }

    public override void Visit(StudyToSeriesStringSingleValueMatchCondition stringSingleValueMatchCondition)
    {
        var queryTag = stringSingleValueMatchCondition.QueryTag;
        var studyDicomTagSqlEntry = DicomTagSqlEntry.StudyKeyDicomTagSqlEntry;
        var seriesDicomTagSqlEntry = DicomTagSqlEntry.GetStudyToSeriesDicomTagSqlEntry(queryTag);

        var tableAlias = GetTableAlias(studyDicomTagSqlEntry, null);
        StringBuilder
            .Append("AND ");
        var seriesTableAlias = "ses";
        StringBuilder
            .Append(studyDicomTagSqlEntry.SqlColumn, tableAlias)
            .Append(" IN  (SELECT DISTINCT StudyKey FROM Series " + seriesTableAlias + " WHERE ")
            .Append(seriesDicomTagSqlEntry.SqlColumn, seriesTableAlias)
            .Append("=")
            .Append(_parameters.AddParameter(seriesDicomTagSqlEntry.SqlColumn, stringSingleValueMatchCondition.Value))
            .Append(" AND ")
            .Append(VLatest.Series.PartitionKey, seriesTableAlias)
            .Append("=")
            .Append(_parameters.AddParameter(VLatest.Series.PartitionKey, PartitionKey))
            .Append(")")
            .AppendLine();
    }

    public override void Visit(DoubleSingleValueMatchCondition doubleSingleValueMatchCondition)
    {
        var queryTag = doubleSingleValueMatchCondition.QueryTag;
        var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag, IsIndexedQueryTag(queryTag));
        var tableAlias = GetTableAlias(dicomTagSqlEntry, GetKeyFromQueryTag(queryTag));
        StringBuilder
            .Append("AND ");

        AppendExtendedQueryTagKeyFilter(dicomTagSqlEntry, tableAlias, doubleSingleValueMatchCondition);

        StringBuilder
            .Append(dicomTagSqlEntry.SqlColumn, tableAlias)
            .Append("=")
            .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, doubleSingleValueMatchCondition.Value))
            .AppendLine();
    }

    public override void Visit(LongRangeValueMatchCondition longRangeValueMatchCondition)
    {
        var queryTag = longRangeValueMatchCondition.QueryTag;
        var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag, IsIndexedQueryTag(queryTag));
        var tableAlias = GetTableAlias(dicomTagSqlEntry, GetKeyFromQueryTag(queryTag));
        StringBuilder
            .Append("AND ");

        AppendExtendedQueryTagKeyFilter(dicomTagSqlEntry, tableAlias, longRangeValueMatchCondition);

        StringBuilder
            .Append(dicomTagSqlEntry.SqlColumn, tableAlias).Append(" BETWEEN ")
            .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, longRangeValueMatchCondition.Minimum.ToString(CultureInfo.InvariantCulture)))
            .Append(" AND ")
            .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, longRangeValueMatchCondition.Maximum.ToString(CultureInfo.InvariantCulture)))
            .AppendLine();
    }

    public override void Visit(LongSingleValueMatchCondition longSingleValueMatchCondition)
    {
        var queryTag = longSingleValueMatchCondition.QueryTag;
        var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag, IsIndexedQueryTag(queryTag));
        var tableAlias = GetTableAlias(dicomTagSqlEntry, GetKeyFromQueryTag(queryTag));
        StringBuilder
            .Append("AND ");

        AppendExtendedQueryTagKeyFilter(dicomTagSqlEntry, tableAlias, longSingleValueMatchCondition);

        StringBuilder
            .Append(dicomTagSqlEntry.SqlColumn, tableAlias)
            .Append("=")
            .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, longSingleValueMatchCondition.Value))
            .AppendLine();
    }

    public override void Visit(DateRangeValueMatchCondition rangeValueMatchCondition)
    {
        var queryTag = rangeValueMatchCondition.QueryTag;
        var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag, IsIndexedQueryTag(queryTag));
        var tableAlias = GetTableAlias(dicomTagSqlEntry, GetKeyFromQueryTag(queryTag));
        StringBuilder
            .Append("AND ");

        AppendExtendedQueryTagKeyFilter(dicomTagSqlEntry, tableAlias, rangeValueMatchCondition);

        StringBuilder
            .Append(dicomTagSqlEntry.SqlColumn, tableAlias).Append(" BETWEEN ")
            .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, rangeValueMatchCondition.Minimum.ToString(SqlDateFormat, CultureInfo.InvariantCulture)))
            .Append(" AND ")
            .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, rangeValueMatchCondition.Maximum.ToString(SqlDateFormat, CultureInfo.InvariantCulture)))
            .AppendLine();
    }

    public override void Visit(DateSingleValueMatchCondition dateSingleValueMatchCondition)
    {
        var queryTag = dateSingleValueMatchCondition.QueryTag;
        var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag, IsIndexedQueryTag(queryTag));
        var tableAlias = GetTableAlias(dicomTagSqlEntry, GetKeyFromQueryTag(queryTag));
        StringBuilder
            .Append("AND ");

        AppendExtendedQueryTagKeyFilter(dicomTagSqlEntry, tableAlias, dateSingleValueMatchCondition);

        StringBuilder
            .Append(dicomTagSqlEntry.SqlColumn, tableAlias)
            .Append("=")
            .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, dateSingleValueMatchCondition.Value.ToString(SqlDateFormat, CultureInfo.InvariantCulture)))
            .AppendLine();
    }

    public override void Visit(PersonNameFuzzyMatchCondition fuzzyMatchCondition)
    {
        var queryTag = fuzzyMatchCondition.QueryTag;
        var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag, IsIndexedQueryTag(queryTag));
        var tableAlias = GetTableAlias(dicomTagSqlEntry, GetKeyFromQueryTag(queryTag));

        var fuzzyMatchString = $"\"{fuzzyMatchCondition.Value}*\"";
        StringBuilder
            .Append("AND ");

        AppendExtendedQueryTagKeyFilter(dicomTagSqlEntry, tableAlias, fuzzyMatchCondition);
        StringBuilder
            .Append("CONTAINS(")
            .Append(tableAlias)
            .Append(".")
            .Append(dicomTagSqlEntry.FullTextIndexColumnName)
            .Append(", ")
            .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, fuzzyMatchString))
            .Append(")")
            .AppendLine();
    }

    protected void AppendPartitionJoinClause(string tableAlias1, string tableAlias2)
    {
        if ((int)SchemaVersion >= SchemaVersionConstants.SupportDataPartitionSchemaVersion)
        {
            StringBuilder.AppendLine($"AND {tableAlias1}.{VLatest.Partition.PartitionKey} = {tableAlias2}.{VLatest.Partition.PartitionKey}");
        }
    }

    protected void AppendPartitionWhereClause(string tableAlias)
    {
        if ((int)SchemaVersion >= SchemaVersionConstants.SupportDataPartitionSchemaVersion)
        {
            StringBuilder.AppendLine($"AND {tableAlias}.{VLatest.Partition.PartitionKey} = {PartitionKey}");
        }
    }

    protected void AppendFilterClause()
    {
        foreach (var filterCondition in QueryExpression.FilterConditions)
        {
            filterCondition.Accept(this);
        }
    }

    private void AppendExtendedQueryTagKeyFilter(DicomTagSqlEntry dicomTagSqlEntry, string tableAlias, QueryFilterCondition filterCondition)
    {
        if (dicomTagSqlEntry.IsIndexedQueryTag)
        {
            StringBuilder
                .Append(dicomTagSqlEntry.SqlKeyColumn, tableAlias)
                .Append("=")
                .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlKeyColumn, GetKeyFromQueryTag(filterCondition.QueryTag)))
                .AppendLine()
                .Append("AND ");
        }
    }

    private static string GetTableAlias(DicomTagSqlEntry sqlEntry, int? extendedQueryTagKey)
    {
        string ret = null;
        switch (sqlEntry.SqlTableType)
        {
            case SqlTableType.InstanceTable:
                ret = InstanceTableAlias;
                break;
            case SqlTableType.StudyTable:
                ret = StudyTableAlias;
                break;
            case SqlTableType.SeriesTable:
                ret = SeriesTableAlias;
                break;
            case SqlTableType.ExtendedQueryTagLongTable:
                ret = ExtendedQueryTagLongTableAlias;
                break;
            case SqlTableType.ExtendedQueryTagDateTimeTable:
                ret = ExtendedQueryTagDateTimeTableAlias;
                break;
            case SqlTableType.ExtendedQueryTagDoubleTable:
                ret = ExtendedQueryTagDoubleTableAlias;
                break;
            case SqlTableType.ExtendedQueryTagPersonNameTable:
                ret = ExtendedQueryTagPersonNameTableAlias;
                break;
            case SqlTableType.ExtendedQueryTagStringTable:
                ret = ExtendedQueryTagStringTableAlias;
                break;
        }

        if (string.IsNullOrEmpty(ret))
        {
            Debug.Fail("Invalid table type");
            return null;
        }

        return ret + extendedQueryTagKey;
    }
}
