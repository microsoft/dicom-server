// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.Core.Messages.Query
{
    public class QueryDicomResourcesRequest : IRequest<QueryDicomResourcesResponse>
    {
        public ResourceType ResourceType { get; }

        public string StudyInstanceUID { get; }

        public string SeriesInstanceUID { get; }

        public string SopInstanceUID { get; }

        public IEnumerable<(DicomAttributeId Attribute, string Value)> Query { get; }

        public bool AllOptionalAttributesRequired { get; }

        public HashSet<DicomAttributeId> OptionalAttributes { get; }

        public bool FuzzyMatching { get; }

        public int Limit { get; }

        public int Offset { get; }
    }
}
