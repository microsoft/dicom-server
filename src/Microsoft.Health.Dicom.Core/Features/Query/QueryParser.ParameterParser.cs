// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    /// <summary>
    /// Parameter parsers
    /// </summary>
    public partial class QueryParser
    {
        private void ParseIncludeField(KeyValuePair<string, StringValues> queryParameter)
        {
            // Check if `all` is present as one of the values in IncludeField parameter.
            if (queryParameter.Value.Any(val => IncludeFieldValueAll.Equals(val.Trim(), QueryParameterComparision)))
            {
                _parsedQuery.AllValue = true;
                return;
            }

            foreach (string value in queryParameter.Value)
            {
                var trimmedValue = value.Trim();
                if (DicomAttributeId.TryParse(trimmedValue, out DicomAttributeId attributeId))
                {
                    _parsedQuery.IncludeFields.Add(attributeId);
                    continue;
                }

                throw new QueryParseException(string.Format(DicomCoreResource.IncludeFieldUnknownAttribute, trimmedValue));
            }
        }

        private void ParseFuzzyMatching(KeyValuePair<string, StringValues> queryParameter)
        {
            var trimmedValue = queryParameter.Value.FirstOrDefault()?.Trim();
            if (bool.TryParse(trimmedValue, out bool result))
            {
                _parsedQuery.FuzzyMatch = result;
            }
            else
            {
                throw new QueryParseException(string.Format(DicomCoreResource.InvalidFuzzyMatchValue, trimmedValue));
            }
        }

        public void ParseOffset(KeyValuePair<string, StringValues> queryParameter)
        {
            var trimmedValue = queryParameter.Value.FirstOrDefault()?.Trim();
            if (int.TryParse(trimmedValue, out int result) && result >= 0)
            {
                _parsedQuery.Offset = result;
            }
            else
            {
                throw new QueryParseException(string.Format(DicomCoreResource.InvalidOffsetValue, trimmedValue));
            }
        }

        private void ParseLimit(KeyValuePair<string, StringValues> queryParameter)
        {
            var trimmedValue = queryParameter.Value.FirstOrDefault()?.Trim();
            if (int.TryParse(trimmedValue, out int result))
            {
                if (result > QueryLimit.MaxQueryResultCount || result < 1)
                {
                    throw new QueryParseException(string.Format(DicomCoreResource.QueryResultCountMaxExceeded, result, 1, QueryLimit.MaxQueryResultCount));
                }

                _parsedQuery.Limit = result;
            }
            else
            {
                throw new QueryParseException(string.Format(DicomCoreResource.InvalidLimitValue, trimmedValue));
            }
        }
    }
}
