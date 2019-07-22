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
        private const string StudySqlQuerySearchFormat = "SELECT DISTINCT VALUE {{ \"" + nameof(DicomStudy.StudyInstanceUID) + "\": c." + DocumentProperties.StudyInstanceUID + " }} FROM c {0} OFFSET " + OffsetParameterName + " LIMIT " + LimitParameterName;
        private const string SeriesSqlQuerySearchFormat = "SELECT VALUE {{ \"" + nameof(DicomSeries.StudyInstanceUID) + "\": c." + DocumentProperties.StudyInstanceUID + ", \"" + nameof(DicomSeries.SeriesInstanceUID) + "\": c." + DocumentProperties.SeriesInstanceUID + " }} FROM c {0} OFFSET " + OffsetParameterName + " LIMIT " + LimitParameterName;
        private const string InstanceSqlQuerySearchFormat = "SELECT VALUE {{ \"" + nameof(DicomInstance.StudyInstanceUID) + "\": c." + DocumentProperties.StudyInstanceUID + ", \"" + nameof(DicomInstance.SeriesInstanceUID) + "\": c." + DocumentProperties.SeriesInstanceUID + ", \"" + nameof(DicomInstance.SopInstanceUID) + "\": f." + DocumentProperties.SopInstanceUID + " }} FROM c JOIN f in c." + DocumentProperties.Instances + " {0} OFFSET " + OffsetParameterName + " LIMIT " + LimitParameterName;
        private readonly DicomIndexingConfiguration _dicomConfiguration;
        private readonly IFormatProvider _stringFormatProvider;

        public CosmosQueryBuilder(DicomIndexingConfiguration dicomConfiguration)
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
            return GenerateQuerySpec(
                InstanceSqlQuerySearchFormat,
                offset,
                limit,
                query,
                (tag, parameter) => $"ARRAY_CONTAINS(f.{DocumentProperties.Attributes}[\"{tag.AttributeId}\"], {parameter.Name})");
        }

        private SqlQuerySpec BuildSeriesLevelQuerySpec(
            string querySearchFormat, int offset, int limit, IEnumerable<(DicomAttributeId Attribute, string Value)> query)
        {
            return GenerateQuerySpec(
                querySearchFormat,
                offset,
                limit,
                query,
                (tag, parameter) => $"ARRAY_CONTAINS(c.{DocumentProperties.DistinctAttributes}[\"{tag.AttributeId}\"][\"{DocumentProperties.Values}\"], {parameter.Name})");
        }

        private SqlQuerySpec GenerateQuerySpec(
            string querySearchFormat,
            int offset,
            int limit,
            IEnumerable<(DicomAttributeId Attribute, string Value)> query,
            Func<DicomAttributeId, SqlParameter, string> createQueryItem)
        {
            // As 'OFFSET' and 'LIMIT' are not supported in Linq, all queries must be run using SQL syntax.
            SqlParameterCollection sqlParameterCollection = CreateQueryParameterCollection(offset, limit);

            var parameterNameIndex = 1;
            var queryItems = new List<string>();

            if (query != null && _dicomConfiguration.QueryAttributes != null)
            {
                foreach ((DicomAttributeId attribute, string value) in query.Where(x => _dicomConfiguration.QueryAttributes.Contains(x.Attribute)))
                {
                    var parameterName = string.Format(_stringFormatProvider, ItemParameterNameFormat, parameterNameIndex++);
                    var sqlParameter = new SqlParameter { Name = parameterName, Value = value };

                    sqlParameterCollection.Add(sqlParameter);
                    queryItems.Add(createQueryItem(attribute, sqlParameter));
                }
            }

            // Now construct the WHERE query joining each item with an AND.
            var whereClause = queryItems.Count > 0 ? $"WHERE {string.Join(" AND ", queryItems)}" : string.Empty;
            return new SqlQuerySpec(string.Format(_stringFormatProvider, querySearchFormat, whereClause), sqlParameterCollection);
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
