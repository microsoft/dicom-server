// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Azure.Documents;
using Microsoft.Health.Dicom.CosmosDb.Config;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage.Documents;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Storage
{
    internal class CosmosQueryBuilder
    {
        private const string OffsetParameterName = "@offset";
        private const string LimitParameterName = "@limit";
        private const string ItemParameterNameFormat = "@item{0}";
        private const string InstanceSqlQuerySearchFormat = "SELECT value {{ \"StudyInstanceUID\": c.StudyInstanceUID, \"SeriesInstanceUID\": c.SeriesInstanceUID, \"SOPInstanceUID\": f.SopInstanceUID }} FROM c JOIN f in c.Instances {0} OFFSET " + OffsetParameterName + " LIMIT " + LimitParameterName;
        private readonly DicomCosmosConfiguration _dicomConfiguration;
        private readonly IFormatProvider _stringFormatProvider;

        public CosmosQueryBuilder(DicomCosmosConfiguration dicomConfiguration)
        {
            EnsureArg.IsNotNull(dicomConfiguration, nameof(dicomConfiguration));

            _dicomConfiguration = dicomConfiguration;
            _stringFormatProvider = CultureInfo.InvariantCulture;
        }

        public SqlQuerySpec BuildInstanceQuerySpec(
            int offset,
            int limit,
            IEnumerable<(DicomTag Attribute, string Value)> query = null)
        {
            // As 'OFFSET' and 'LIMIT' are not supported in Linq, all queries must be run using SQL syntax.
            var sqlParameterCollection = new SqlParameterCollection()
            {
                new SqlParameter { Name = OffsetParameterName, Value = offset },
                new SqlParameter { Name = LimitParameterName, Value = limit },
            };

            // If a null or empty query collection we should provide an empty string for the WHERE clause.
            var whereClause = string.Empty;

            if (query != null && query.Any())
            {
                var parameterNameIndex = 1;
                var whereItems = new List<string>();

                foreach ((DicomTag attribute, string value) in query.Where(x => _dicomConfiguration.QueryAttributes.Contains(x.Attribute)))
                {
                    var parameterName = string.Format(_stringFormatProvider, ItemParameterNameFormat, parameterNameIndex++);
                    sqlParameterCollection.Add(new SqlParameter { Name = parameterName, Value = value });
                    whereItems.Add($"f.{nameof(QueryInstance.IndexedAttributes)}[\"{DicomTagSerializer.Serialize(attribute)}\"] = {parameterName}");
                }

                // Now construct the WHERE query joining each item with an AND.
                whereClause = $"WHERE {string.Join(" AND ", whereItems)}";
            }

            return new SqlQuerySpec(string.Format(_stringFormatProvider, InstanceSqlQuerySearchFormat, whereClause.ToString(_stringFormatProvider)), sqlParameterCollection);
        }
    }
}
