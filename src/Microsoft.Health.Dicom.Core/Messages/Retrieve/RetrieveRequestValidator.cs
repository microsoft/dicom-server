// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve;

public static class RetrieveRequestValidator
{
    private const string UnknownDicomTransferSyntaxName = "Unknown";
    private const string StudyInstanceUid = "StudyInstanceUid";
    private const string SeriesInstanceUid = "SeriesInstanceUid";
    private const string SopInstanceUid = "SopInstanceUid";

    public static void ValidateInstanceIdentifiers(ResourceType resourceType, string studyInstanceUid, string seriesInstanceUid = null, string sopInstanceUid = null)
    {
        EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

        ValidateInstanceIdentifiersAreValid(resourceType, studyInstanceUid, seriesInstanceUid, sopInstanceUid);
    }

    private static void ValidateInstanceIdentifiersAreValid(ResourceType resourceType, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
    {
        UidValidation.Validate(studyInstanceUid, nameof(StudyInstanceUid));

        switch (resourceType)
        {
            case ResourceType.Series:
                UidValidation.Validate(seriesInstanceUid, nameof(SeriesInstanceUid));
                break;
            case ResourceType.Instance:
            case ResourceType.Frames:
                UidValidation.Validate(seriesInstanceUid, nameof(SeriesInstanceUid));
                UidValidation.Validate(sopInstanceUid, nameof(SopInstanceUid));
                break;
        }
    }

    public static void ValidateFrames(IReadOnlyCollection<int> frames)
    {
        if (frames == null || frames.Count == 0 || frames.Any(x => x < 0))
            throw new BadRequestException(DicomCoreResource.InvalidFramesValue);

    }

    public static void ValidateTransferSyntax(string requestedTransferSyntax, bool originalTransferSyntaxRequested = false)
    {
        if (!originalTransferSyntaxRequested && !string.IsNullOrEmpty(requestedTransferSyntax))
        {
            try
            {
                DicomTransferSyntax transferSyntax = DicomTransferSyntax.Parse(requestedTransferSyntax);

                if (transferSyntax?.UID == null || transferSyntax.UID.Name == UnknownDicomTransferSyntaxName)
                {
                    throw new BadRequestException(DicomCoreResource.InvalidTransferSyntaxValue);
                }
            }
            catch (DicomDataException)
            {
                throw new BadRequestException(DicomCoreResource.InvalidTransferSyntaxValue);
            }
        }
    }
}
