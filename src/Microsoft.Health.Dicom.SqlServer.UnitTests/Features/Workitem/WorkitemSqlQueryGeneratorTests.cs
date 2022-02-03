// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using FellowOakDicom;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.SqlServer.Features.Workitem;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Features.Storage;
using Xunit;

namespace Microsoft.Health.Dicom.SqlServer.UnitTests.Features.Query
{
    public class WorkitemSqlQueryGeneratorTests
    {
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
            new WorkitemSqlQueryGenerator(stringBuilder, query, parm, SqlServer.Features.Schema.SchemaVersion.V9, DefaultPartition.Key);

            string expectedExtendedQueryTagTableFilter = @"INNER JOIN dbo.ExtendedQueryTagString cts1
ON cts1.PartitionKey = w.PartitionKey
AND cts1.ResourceType = 1
AND cts1.SopInstanceKey1 = w.WorkitemKey
WHERE";

            string expectedFilters = @"AND cts1.TagKey=@p0
AND cts1.TagValue=@p1";

            string builtString = stringBuilder.ToString();
            Assert.Equal(queryTag.WorkitemQueryTagStoreEntry.Key.ToString(), sqlParameterCollection[0].Value.ToString());
            Assert.Equal(filter.Value.ToString(), sqlParameterCollection[1].Value.ToString());
            Assert.Contains(expectedExtendedQueryTagTableFilter, builtString);
            Assert.Contains(expectedFilters, builtString);
        }

        private SqlParameterCollection CreateSqlParameterCollection()
        {
            return (SqlParameterCollection)typeof(SqlParameterCollection).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null).Invoke(null);
        }
    }
}
