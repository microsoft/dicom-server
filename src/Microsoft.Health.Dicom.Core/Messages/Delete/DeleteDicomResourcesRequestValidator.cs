// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FluentValidation;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Messages.Delete
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Follows validator naming convention.")]
    public class DeleteDicomResourcesRequestValidator : AbstractValidator<DeleteDicomResourcesRequest>
    {
        public DeleteDicomResourcesRequestValidator()
        {
            // Only validate the UIDs when provided.
            RuleFor(x => x.StudyInstanceUID)
                .SetValidator(new DicomIdentifierValidator());

            RuleFor(x => x.SeriesUID)
                .SetValidator(new DicomIdentifierValidator())
                .When(x => x.ResourceType != ResourceType.Study);

            RuleFor(x => x.InstanceUID)
                .SetValidator(new DicomIdentifierValidator())
                .When(x => x.ResourceType == ResourceType.Instance);
        }
    }
}
