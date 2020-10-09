// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class DateRangeValueMatchCondition : RangeValueMatchCondition<DateTime>
    {
        internal DateRangeValueMatchCondition(DicomAttributeId attributeId, DateTime minimum, DateTime maximum)
            : base(attributeId, minimum, maximum)
        {
        }

        public override void Accept(QueryFilterConditionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
