// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FluentValidation;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Messages.Store
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Follows validator naming convention.")]
    public class StoreDicomResourcesRequestValidator : AbstractValidator<StoreDicomResourcesRequest>
    {
        public StoreDicomResourcesRequestValidator()
        {
            // Only validate the study instance UID when provided.
            RuleFor(x => x.StudyInstanceUID)
                .SetValidator(new UniqueIdentifierValidator())
                .When(x => x.StudyInstanceUID != null);
        }
    }
}
