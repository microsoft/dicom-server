// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public abstract class SingleValueMatchCondition<T> : QueryFilterCondition
    {
        internal SingleValueMatchCondition(DicomAttributeId attributeid, T value)
            : base(attributeid)
        {
            Value = value;
        }

        public T Value { get; }
    }
}
