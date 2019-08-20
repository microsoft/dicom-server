// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FluentValidation;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Messages.Query
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Follows validator naming convention.")]
    public class QueryDicomResourcesRequestValidator : AbstractValidator<QueryDicomResourcesRequest>
    {
        private const int MaximumStudySeriesLimit = 1000;
        private const int MaximumInstanceLimit = 10000;

        public QueryDicomResourcesRequestValidator()
        {
            // We cannot query frames
            RuleFor(x => x.ResourceType)
                .Must(x => x != ResourceType.Frames);

            // Check the limit is between 0 and the limit depending on the requested query type.
            RuleFor(x => x.Limit)
                .GreaterThan(0).LessThanOrEqualTo(MaximumStudySeriesLimit)
                .When(x => x.ResourceType != ResourceType.Instance);
            RuleFor(x => x.Limit)
                .GreaterThan(0).LessThanOrEqualTo(MaximumInstanceLimit)
                .When(x => x.ResourceType == ResourceType.Instance);

            RuleFor(x => x.Offset)
                .GreaterThanOrEqualTo(0);

            // The study instance identifier should be valid if the series instance UID has been provided or is not null or whitespace.
            RuleFor(x => x.StudyInstanceUID)
                .SetValidator(new DicomIdentifierValidator())
                .When(x => !string.IsNullOrWhiteSpace(x.SeriesInstanceUID) || !string.IsNullOrWhiteSpace(x.StudyInstanceUID));

            // The series instance identifier should be valid if provided.
            RuleFor(x => x.SeriesInstanceUID)
                .SetValidator(new DicomIdentifierValidator())
                .When(x => !string.IsNullOrWhiteSpace(x.SeriesInstanceUID));

            // Validate all the query and optional attributes are valid and can be parsed.
            RuleForEach(x => x.QueryAttributeValues)
                .Must(x => DicomAttributeId.IsValidAttributeId(x.Key));

            // Check all optional attributes parse to an attribute ID or is 'all'.
            RuleForEach(x => x.OptionalAttributes)
                .Must(x =>
                    string.Equals(x, QueryDicomResourcesRequest.AllOptionalAttributesRequestedString, StringComparison.InvariantCultureIgnoreCase) ||
                    DicomAttributeId.IsValidAttributeId(x));
        }
    }
}
