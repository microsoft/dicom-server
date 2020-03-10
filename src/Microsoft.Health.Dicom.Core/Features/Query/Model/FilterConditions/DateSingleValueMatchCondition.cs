// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class DateSingleValueMatchCondition : SingleValueMatchCondition<DateTime>
    {
        internal DateSingleValueMatchCondition(DicomTag tag, DateTime value)
            : base(tag, value)
        {
        }

        public override void Accept(QueryFilterConditionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
