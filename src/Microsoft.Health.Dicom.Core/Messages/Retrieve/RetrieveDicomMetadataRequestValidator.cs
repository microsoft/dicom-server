// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FluentValidation;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Follows validator naming convention.")]
    public class RetrieveDicomMetadataRequestValidator : AbstractValidator<RetrieveDicomMetadataRequest>
    {
        public RetrieveDicomMetadataRequestValidator()
        {
            // Validate the provided identifiers conform correctly.
            RuleFor(x => x.SopInstanceUID)
                .SetValidator(new DicomIdentifierValidator())
                .When(x => x.ResourceType == ResourceType.Instance);
            RuleFor(x => x.SeriesInstanceUID)
                .SetValidator(new DicomIdentifierValidator())
                .When(x => x.ResourceType != ResourceType.Study);
            RuleFor(x => x.StudyInstanceUID)
                .SetValidator(new DicomIdentifierValidator());

            // Check for non-repeated identifiers.
            RuleFor(x => x)
                .Must(x => x.StudyInstanceUID != x.SeriesInstanceUID && x.StudyInstanceUID != x.SopInstanceUID);
            RuleFor(x => x)
                .Must(x => x.SeriesInstanceUID != x.SopInstanceUID)
                .When(x => x.ResourceType != ResourceType.Study);
        }
    }
}
