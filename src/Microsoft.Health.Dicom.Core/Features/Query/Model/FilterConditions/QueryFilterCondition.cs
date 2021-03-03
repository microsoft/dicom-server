// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public abstract class QueryFilterCondition
    {
        protected QueryFilterCondition(DicomTag tag)
        {
            DicomTag = tag;
        }

        public DicomTag DicomTag { get; }

        public CustomTagFilterDetails CustomTagFilterDetails { get; set; }

        public abstract void Accept(QueryFilterConditionVisitor visitor);
    }
}
