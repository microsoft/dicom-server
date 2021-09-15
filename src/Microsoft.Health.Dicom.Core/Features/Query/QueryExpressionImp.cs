// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    internal class QueryExpressionImp
    {
        public QueryExpressionImp()
        {
            IncludeFields = new HashSet<DicomTag>();
            FilterConditions = new List<QueryFilterCondition>();
        }

        public HashSet<DicomTag> IncludeFields { get; set; }

        public bool FuzzyMatch { get; set; }

        public int Offset { get; set; }

        public int Limit { get; set; }

        public List<QueryFilterCondition> FilterConditions { get; set; }

        public bool AllValue { get; set; }

    }
}
