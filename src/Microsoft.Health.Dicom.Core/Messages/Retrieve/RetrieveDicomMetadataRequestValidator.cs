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
            RuleFor(x => x.SopInstanceUid)
                .Must(DicomIdentifierValidator.Validate)
                .When(x => x.ResourceType == ResourceType.Instance);
            RuleFor(x => x.SeriesInstanceUid)
                 .Must(DicomIdentifierValidator.Validate)
                .When(x => x.ResourceType != ResourceType.Study);
            RuleFor(x => x.StudyInstanceUid)
                 .Must(DicomIdentifierValidator.Validate);

            // Check for non-repeated identifiers.
            RuleFor(x => x)
                .Must(x => x.StudyInstanceUid != x.SeriesInstanceUid && x.StudyInstanceUid != x.SopInstanceUid);
            RuleFor(x => x)
                .Must(x => x.SeriesInstanceUid != x.SopInstanceUid)
                .When(x => x.ResourceType != ResourceType.Study);
        }
    }
}
