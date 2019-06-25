// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnsureThat;
using Microsoft.Azure.Documents;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.CosmosDb.Config;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage.Documents;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Storage
{
    internal class CosmosQueryBuilder
    {
        private const string OffsetParameterName = "@offset";
        private const string LimitParameterName = "@limit";
        private const string ItemParameterNameFormat = "@item{0}";
        private const string StudySqlQuerySearchFormat = "SELECT DISTINCT c.StudyInstanceUID FROM c {0} OFFSET " + OffsetParameterName + " LIMIT " + LimitParameterName;
        private const string SeriesSqlQuerySearchFormat = "SELECT VALUE {{ \"StudyInstanceUID\": c.StudyInstanceUID, \"SeriesInstanceUID\": c.SeriesInstanceUID }} FROM c {0} OFFSET " + OffsetParameterName + " LIMIT " + LimitParameterName;
        private const string InstanceSqlQuerySearchFormat = "SELECT VALUE {{ \"StudyInstanceUID\": c.StudyInstanceUID, \"SeriesInstanceUID\": c.SeriesInstanceUID, \"SOPInstanceUID\": f.SopInstanceUID }} FROM c JOIN f in c.Instances {0} OFFSET " + OffsetParameterName + " LIMIT " + LimitParameterName;
        private readonly DicomCosmosConfiguration _dicomConfiguration;
        private readonly IFormatProvider _stringFormatProvider;

        public CosmosQueryBuilder(DicomCosmosConfiguration dicomConfiguration)
        {
            EnsureArg.IsNotNull(dicomConfiguration, nameof(dicomConfiguration));

            _dicomConfiguration = dicomConfiguration;
            _stringFormatProvider = CultureInfo.InvariantCulture;
        }

        public SqlQuerySpec BuildStudyQuerySpec(
            int offset, int limit, IEnumerable<(DicomAttributeId Attribute, string Value)> query = null)
                => BuildSeriesLevelQuerySpec(StudySqlQuerySearchFormat, offset, limit, query);

        public SqlQuerySpec BuildSeriesQuerySpec(
            int offset, int limit, IEnumerable<(DicomAttributeId Attribute, string Value)> query = null)
                => BuildSeriesLevelQuerySpec(SeriesSqlQuerySearchFormat, offset, limit, query);

        public SqlQuerySpec BuildInstanceQuerySpec(
            int offset, int limit, IEnumerable<(DicomAttributeId Attribute, string Value)> query = null)
        {
            // As 'OFFSET' and 'LIMIT' are not supported in Linq, all queries must be run using SQL syntax.
            SqlParameterCollection sqlParameterCollection = CreateQueryParameterCollection(offset, limit);

            string whereClause = GenerateWhereClause(
                query,
                (tag, parameter) =>
                {
                    sqlParameterCollection.Add(parameter);
                    return $"ARRAY_CONTAINS(f.{nameof(QueryInstance.IndexedAttributes)}[\"{tag.AttributeId}\"], {parameter.Name})";
                });

            return new SqlQuerySpec(string.Format(_stringFormatProvider, InstanceSqlQuerySearchFormat, whereClause.ToString(_stringFormatProvider)), sqlParameterCollection);
        }

        private SqlQuerySpec BuildSeriesLevelQuerySpec(
            string querySearchFormat, int offset, int limit, IEnumerable<(DicomAttributeId Attribute, string Value)> query)
        {
            // As 'OFFSET' and 'LIMIT' are not supported in Linq, all queries must be run using SQL syntax.
            SqlParameterCollection sqlParameterCollection = CreateQueryParameterCollection(offset, limit);

            string whereClause = GenerateWhereClause(
                query,
                (tag, parameter) =>
                {
                    sqlParameterCollection.Add(parameter);
                    return $"ARRAY_CONTAINS(c.{nameof(QuerySeriesDocument.DistinctIndexedAttributes)}[\"{tag.AttributeId}\"].{nameof(AttributeValues.Values)}, {parameter.Name})";
                });

            return new SqlQuerySpec(string.Format(_stringFormatProvider, querySearchFormat, whereClause.ToString(_stringFormatProvider)), sqlParameterCollection);
        }

        private string GenerateWhereClause(IEnumerable<(DicomAttributeId Attribute, string Value)> query, Func<DicomAttributeId, SqlParameter, string> createQueryItem)
        {
            // If a null or empty query collection we should provide an empty string for the WHERE clause.
            if (query == null || !query.Any())
            {
                return string.Empty;
            }

            var parameterNameIndex = 1;
            var queryItems = new List<string>();

            foreach ((DicomAttributeId attribute, string value) in query.Where(x => _dicomConfiguration.QueryAttributes.Contains(x.Attribute)))
            {
                var parameterName = string.Format(_stringFormatProvider, ItemParameterNameFormat, parameterNameIndex++);
                queryItems.Add(createQueryItem(attribute, new SqlParameter { Name = parameterName, Value = value }));
            }

            // Now construct the WHERE query joining each item with an AND.
            return $"WHERE {string.Join(" AND ", queryItems)}";
        }

        private static SqlParameterCollection CreateQueryParameterCollection(int offset, int limit)
        {
            EnsureArg.IsGte(offset, 0, nameof(offset));
            EnsureArg.IsGt(limit, 0, nameof(limit));

            return new SqlParameterCollection()
            {
                new SqlParameter { Name = OffsetParameterName, Value = offset },
                new SqlParameter { Name = LimitParameterName, Value = limit },
            };
        }
    }
}
