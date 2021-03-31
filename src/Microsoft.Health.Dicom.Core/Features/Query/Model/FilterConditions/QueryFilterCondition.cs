// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public abstract class QueryFilterCondition
    {
        protected QueryFilterCondition(QueryTag queryTag)
        {
            EnsureArg.IsNotNull(queryTag, nameof(queryTag));
            QueryTag = queryTag;
        }



        public QueryTag QueryTag { get; set; }

        public abstract void Accept(QueryFilterConditionVisitor visitor);
    }
}
