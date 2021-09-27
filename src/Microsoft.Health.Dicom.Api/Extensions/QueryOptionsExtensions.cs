// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Api.Models;
using Microsoft.Health.Dicom.Core.Features.Query;

namespace Microsoft.Health.Dicom.Api.Extensions
{
    internal static class QueryOptionsExtensions
    {
        private static readonly ImmutableHashSet<string> KnownParameters = ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            nameof(QueryOptions.FuzzyMatching),
            nameof(QueryOptions.IncludeField),
            nameof(QueryOptions.Limit),
            nameof(QueryOptions.Offset));

        public static QueryParameters ToParameters(
            this QueryOptions options,
            IEnumerable<KeyValuePair<string, StringValues>> queryString,
            QueryResource resourceType,
            string studyInstanceUid = null,
            string seriesInstanceUid = null)
        {
            // Parse the remaining query-string parameters into a dictionary
            var filters = new Dictionary<string, string>();
            foreach (KeyValuePair<string, StringValues> qsp in queryString)
            {
                string attributeId = qsp.Key.Trim();
                if (!KnownParameters.Contains(attributeId))
                {
                    if (qsp.Value.Count > 1)
                    {
                        throw new QueryParseException(string.Format(DicomApiResource.DuplicateAttributeId, attributeId));
                    }

                    // No need to also check for duplicate keys as they are aggregated together in StringValues
                    filters.Add(attributeId, qsp.Value[0].Trim());
                }
            }

            return new QueryParameters
            {
                Filters = filters,
                FuzzyMatching = options.FuzzyMatching,
                IncludeField = options.IncludeField,
                Limit = options.Limit,
                Offset = options.Offset,
                QueryResourceType = resourceType,
                SeriesInstanceUid = seriesInstanceUid,
                StudyInstanceUid = studyInstanceUid,
            };
        }
    }
}
