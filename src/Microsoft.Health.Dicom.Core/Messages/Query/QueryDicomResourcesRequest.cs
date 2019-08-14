// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.Core.Messages.Query
{
    public class QueryDicomResourcesRequest : IRequest<QueryDicomResourcesResponse>
    {
        private const string AllOptionalAttributesRequestedString = "all";
        private const int DefaultQueryStudySeriesLimit = 100;
        private const int DefaultQueryInstanceLimit = 1000;

        public QueryDicomResourcesRequest(
            ResourceType resourceType,
            IEnumerable<KeyValuePair<string, string>> queryAttributeValues,
            string[] optionalAttributes,
            bool fuzzyMatching,
            int? limit,
            int offset,
            string studyInstanceUID,
            string seriesInstanceUID)
        {
            ResourceType = resourceType;
            QueryAttributeValues = queryAttributeValues;
            OptionalAttributes = optionalAttributes;
            AllOptionalAttributesRequired = optionalAttributes.Any(x => string.Equals(x, AllOptionalAttributesRequestedString, StringComparison.InvariantCultureIgnoreCase));
            FuzzyMatching = fuzzyMatching;
            Limit = limit ?? (resourceType == ResourceType.Instance ? DefaultQueryInstanceLimit : DefaultQueryStudySeriesLimit);
            Offset = offset;
            StudyInstanceUID = studyInstanceUID;
            SeriesInstanceUID = seriesInstanceUID;
        }

        public ResourceType ResourceType { get; }

        public IEnumerable<KeyValuePair<string, string>> QueryAttributeValues { get; }

        public IEnumerable<string> OptionalAttributes { get; }

        public bool AllOptionalAttributesRequired { get; }

        public bool FuzzyMatching { get; }

        public int Limit { get; }

        public int Offset { get; }

        public string StudyInstanceUID { get; }

        public string SeriesInstanceUID { get; }

        internal IEnumerable<(DicomAttributeId attributeId, string value)> GetQueryAttributes()
        {
            return QueryAttributeValues.Select(x => (new DicomAttributeId(x.Key), x.Value));
        }

        internal HashSet<DicomAttributeId> GetOptionalAttributes()
        {
            return new HashSet<DicomAttributeId>(OptionalAttributes.Select(x => new DicomAttributeId(x)));
        }
    }
}
