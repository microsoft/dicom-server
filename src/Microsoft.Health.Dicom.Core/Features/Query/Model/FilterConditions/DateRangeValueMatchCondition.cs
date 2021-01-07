// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class DateRangeValueMatchCondition : RangeValueMatchCondition<DateTime>
    {
        internal DateRangeValueMatchCondition(DicomTag tag, DateTime minimum, DateTime maximum)
            : base(tag, minimum, maximum)
        {
        }

        public override void Accept(QueryFilterConditionVisitor visitor)
        {
            EnsureArg.IsNotNull(visitor, nameof(visitor));
            visitor.Visit(this);
        }
    }
}
