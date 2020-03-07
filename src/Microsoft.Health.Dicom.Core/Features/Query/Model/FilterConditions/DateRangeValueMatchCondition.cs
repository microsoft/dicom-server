// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class DateRangeValueMatchCondition : DicomQueryFilterCondition
    {
        internal DateRangeValueMatchCondition(DicomTag tag, string minimum, string maximum)
            : base(tag)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public string Minimum { get; set; }

        public string Maximum { get; set; }

        public override void Accept(QueryFilterConditionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
