// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    internal static class QueryParamsParser
    {
        private const string IncludeFieldValueAll = "all";
        private const StringComparison QueryParameterComparision = StringComparison.OrdinalIgnoreCase;
        public static void Parse(StaticQueryParams staticQueryParams, QueryExpressionParams expParams)
        {
            EnsureArg.IsNotNull(staticQueryParams, nameof(staticQueryParams));
            EnsureArg.IsNotNull(expParams, nameof(expParams));
            if (staticQueryParams.IncludeField != null)
            {
                ParseIncludeField(staticQueryParams.IncludeField, expParams);
            }
            if (staticQueryParams.FuzzyMatching.HasValue)
            {
                expParams.FuzzyMatch = staticQueryParams.FuzzyMatching.Value;
            }

            if (staticQueryParams.Limit.HasValue)
            {
                expParams.Limit = staticQueryParams.Limit.Value;
            }

            if (staticQueryParams.Offset.HasValue)
            {
                expParams.Offset = staticQueryParams.Offset.Value;
            }
        }

        private static void ParseIncludeField(IReadOnlyList<string> includeField, QueryExpressionParams expParams)
        {
            // Check if `all` is present as one of the values in IncludeField parameter.
            if (includeField.Any(val => IncludeFieldValueAll.Equals(val.Trim(), QueryParameterComparision)))
            {
                expParams.AllValue = true;
                return;
            }

            foreach (string paramValue in includeField)
            {
                foreach (string value in paramValue.Split(','))
                {
                    var trimmedValue = value.Trim();
                    if (DicomTagParser.TryParse(trimmedValue, out DicomTag dicomTag))
                    {
                        expParams.IncludeFields.Add(dicomTag);
                        continue;
                    }

                    throw new QueryParseException(string.Format(DicomCoreResource.IncludeFieldUnknownAttribute, trimmedValue));
                }
            }
        }
    }
}
