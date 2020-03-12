// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class StringSingleValueMatchCondition : SingleValueMatchCondition<string>
    {
        internal StringSingleValueMatchCondition(DicomTag tag, string value)
            : base(tag, value)
        {
        }

        public override void Accept(QueryFilterConditionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
