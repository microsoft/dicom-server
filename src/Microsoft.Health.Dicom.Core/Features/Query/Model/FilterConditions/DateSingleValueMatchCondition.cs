// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class DateSingleValueMatchCondition : SingleValueMatchCondition<DateTime>
    {
        internal DateSingleValueMatchCondition(DicomAttributeId attributeId, DateTime value)
            : base(attributeId, value)
        {
        }

        public override void Accept(QueryFilterConditionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
