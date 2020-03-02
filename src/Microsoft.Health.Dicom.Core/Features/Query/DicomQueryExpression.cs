// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    /// <summary>
    /// Valid parsed object represeting query parameters for a QIDO-RS request
    /// </summary>
    public class DicomQueryExpression
    {
        public DicomQueryExpression(
            DicomQueryParameterIncludeField includeFields,
            bool fuzzyMatching,
            int limit,
            int offset,
            IReadOnlyCollection<DicomQueryFilterCondition> filterConditions)
        {
            IncludeFields = includeFields;
            FuzzyMatching = fuzzyMatching;
            Limit = limit;
            Offset = offset;
            FilterConditions = filterConditions;
        }

        public DicomQueryExpression()
        {
            IsEmpty = true;
        }

        /// <summary>
        /// Dicom tags to include in query result
        /// </summary>
        public DicomQueryParameterIncludeField IncludeFields { get; }

        /// <summary>
        /// If true do Fuzzy matching of PN tag types
        /// </summary>
        public bool FuzzyMatching { get; }

        /// <summary>
        /// Query result count
        /// </summary>
        public int Limit { get; }

        /// <summary>
        /// Query result skip offset count
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// List of filter conditions to find the DICOM objects
        /// </summary>
        public IReadOnlyCollection<DicomQueryFilterCondition> FilterConditions { get; }

        /// <summary>
        /// Request query was empty
        /// </summary>
        public bool IsEmpty { get; }
    }

    public class DicomQueryParameterIncludeField
    {
        public DicomQueryParameterIncludeField(bool all, IReadOnlyCollection<DicomTag> dicomTags)
        {
            All = all;
            DicomTags = dicomTags;
        }

        /// <summary>
        /// If true, include all default and additional fields
        /// DicomTags are ignored if all is true
        /// </summary>
        public bool All { get; }

        /// <summary>
        /// List of additional DicomTags to return with defaults. Used only if "all=false"
        /// </summary>
        public IReadOnlyCollection<DicomTag> DicomTags { get; }
    }
}
