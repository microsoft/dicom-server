// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class DoubleSingleValueMatchCondition : SingleValueMatchCondition<double>
    {
        internal DoubleSingleValueMatchCondition(DicomTag tag, double value)
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
