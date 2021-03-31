// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class DateSingleValueMatchCondition : SingleValueMatchCondition<DateTime>
    {
        internal DateSingleValueMatchCondition(QueryTag tag, DateTime value)
            : base(tag, value)
        {
        }

        public override void Accept(QueryFilterConditionVisitor visitor)
        {
            EnsureArg.IsNotNull(visitor, nameof(visitor));
            visitor.Visit(this);
        }
    }
}
