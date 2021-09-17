// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;

namespace Microsoft.Health.Dicom.Tests.Common
{
    public static class TestObjectFactory
    {
        public static QueryExpression CreateQueryExpression(
            QueryIncludeField includeFields,
            IReadOnlyCollection<QueryFilterCondition> filterConditions,
            QueryResource resourceType = QueryResource.AllStudies,
            bool fuzzyMatching = false,
            int limit = 0,
            int offset = 0)
        {
            return new QueryExpression(resourceType, includeFields, fuzzyMatching, limit, offset, filterConditions);
        }
    }
}
