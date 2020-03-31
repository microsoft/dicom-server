// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using Dicom;
using FluentValidation;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Follows validator naming convention.")]
    public class RetrieveDicomResourcesRequestValidator : AbstractValidator<RetrieveDicomResourceRequest>
    {
        private const string UnknownDicomTransferSyntaxName = "Unknown";

        public RetrieveDicomResourcesRequestValidator()
        {
            // Only validate the requested transfer syntax when provided.
            RuleFor(x => x.RequestedRepresentation)
                .Must(x =>
                {
                    try
                    {
                        var transferSyntax = DicomTransferSyntax.Parse(x);
                        return transferSyntax?.UID != null && transferSyntax.UID.Name != UnknownDicomTransferSyntaxName;
                    }
                    catch (DicomDataException)
                    {
                        return false;
                    }
                })
                .When(x => !x.OriginalTransferSyntaxRequested() && x.RequestedRepresentation != null);

            // Check the frames has at least one when requested, and all requested frames are >= 0.
            RuleFor(x => x.Frames)
                .Must(x => x != null && x.Any() && x.Any(y => y < 0) == false)
                .When(x => x.ResourceType == ResourceType.Frames);

            // Validate the provided identifiers conform correctly.
            RuleFor(x => x.SopInstanceUid)
                .Must(DicomIdentifierValidator.Validate)
                .When(x => x.ResourceType == ResourceType.Frames || x.ResourceType == ResourceType.Instance);
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
