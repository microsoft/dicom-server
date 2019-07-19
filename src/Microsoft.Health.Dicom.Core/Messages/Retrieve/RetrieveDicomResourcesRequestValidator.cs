// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using FluentValidation;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Follows validator naming convention.")]
    public class RetrieveDicomResourcesRequestValidator : AbstractValidator<RetrieveDicomResourceRequest>
    {
        public RetrieveDicomResourcesRequestValidator()
        {
            // Only validate the requested transfer syntax when provided.
            RuleFor(x => x.RequestedTransferSyntax)
                .Must(x => DicomTransferSyntax.Parse(x) != null)
                .When(x => x.RequestedTransferSyntax != null);
        }
    }
}
