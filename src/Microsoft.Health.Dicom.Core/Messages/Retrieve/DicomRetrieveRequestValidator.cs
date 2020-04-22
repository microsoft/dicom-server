// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve
{
    public static class DicomRetrieveRequestValidator
    {
        private const string UnknownDicomTransferSyntaxName = "Unknown";
        private const string StudyInstanceUid = "StudyInstanceUid";
        private const string SeriesInstanceUid = "SeriesInstanceUid";
        private const string SopInstanceUid = "SopInstanceUid";

        public static void ValidateInstanceIdentifiers(ResourceType resourceType, string studyInstanceUid, string seriesInstanceUid = null, string sopInstanceUid = null)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

            ValidateInstanceIndentifiersAreValid(resourceType, studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            ValidateInstanceIdentifiersAreNotDuplicate(resourceType, studyInstanceUid, seriesInstanceUid, sopInstanceUid);
        }

        private static void ValidateInstanceIndentifiersAreValid(ResourceType resourceType, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            DicomIdentifierValidator.ValidateAndThrow(studyInstanceUid, nameof(StudyInstanceUid));

            switch (resourceType)
            {
                case ResourceType.Series:
                    DicomIdentifierValidator.ValidateAndThrow(seriesInstanceUid, nameof(SeriesInstanceUid));
                    break;
                case ResourceType.Instance:
                case ResourceType.Frames:
                    DicomIdentifierValidator.ValidateAndThrow(seriesInstanceUid, nameof(SeriesInstanceUid));
                    DicomIdentifierValidator.ValidateAndThrow(sopInstanceUid, nameof(SopInstanceUid));
                    break;
            }
        }

        private static void ValidateInstanceIdentifiersAreNotDuplicate(ResourceType resourceType, string studyInstanceUid, string seriesInstanceUid = null, string sopInstanceUid = null)
        {
            switch (resourceType)
            {
                case ResourceType.Series:
                    if (studyInstanceUid == seriesInstanceUid)
                    {
                        throw new DicomBadRequestException(DicomCoreResource.DuplicatedUidsNotAllowed);
                    }

                    break;
                case ResourceType.Frames:
                case ResourceType.Instance:
                    if ((studyInstanceUid == seriesInstanceUid) ||
                        (studyInstanceUid == sopInstanceUid) ||
                        (seriesInstanceUid == sopInstanceUid))
                    {
                        throw new DicomBadRequestException(DicomCoreResource.DuplicatedUidsNotAllowed);
                    }

                    break;
            }
        }

        public static void ValidateFrames(IEnumerable<int> frames)
        {
            if (frames == null || !frames.Any())
            {
                throw new DicomBadRequestException(DicomCoreResource.InvalidFramesValue);
            }

            foreach (var x in frames)
            {
                if (x < 0)
                {
                    throw new DicomBadRequestException(DicomCoreResource.InvalidFramesValue);
                }
            }
        }

        public static void ValidateTransferSyntax(string requestedTransferSyntax, bool originalTransferSyntaxRequested = false)
        {
            if (!originalTransferSyntaxRequested && requestedTransferSyntax != null)
            {
                try
                {
                    var transferSyntax = DicomTransferSyntax.Parse(requestedTransferSyntax);

                    if (transferSyntax?.UID == null || transferSyntax.UID.Name == UnknownDicomTransferSyntaxName)
                    {
                        throw new DicomBadRequestException(DicomCoreResource.InvalidTransferSyntaxValue);
                    }
                }
                catch (DicomDataException)
                {
                    throw new DicomBadRequestException(DicomCoreResource.InvalidTransferSyntaxValue);
                }
            }
        }
    }
}
