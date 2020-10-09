// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public abstract class QueryFilterCondition
    {
        public QueryFilterCondition(DicomAttributeId attributeId)
        {
            AttributeId = attributeId;
        }

        public DicomAttributeId AttributeId { get; }

        public abstract void Accept(QueryFilterConditionVisitor visitor);
    }
}
