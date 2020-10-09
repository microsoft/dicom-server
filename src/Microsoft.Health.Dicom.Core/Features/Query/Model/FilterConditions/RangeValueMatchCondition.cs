// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public abstract class RangeValueMatchCondition<T> : QueryFilterCondition
    {
        internal RangeValueMatchCondition(DicomAttributeId attributeId, T minimum, T maximum)
            : base(attributeId)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public T Minimum { get; set; }

        public T Maximum { get; set; }
    }
}
