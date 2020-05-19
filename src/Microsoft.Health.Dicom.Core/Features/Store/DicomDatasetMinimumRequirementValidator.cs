// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to validate a <see cref="DicomDataset"/> to make sure it meets the minimum requirement.
    /// </summary>
    public class DicomDatasetMinimumRequirementValidator : IDicomDatasetMinimumRequirementValidator
    {
        public void Validate(DicomDataset dicomDataset, string requiredStudyInstanceUid)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            // Ensure required tags are present.
            EnsureRequiredTagIsPresent(DicomTag.PatientID);
            EnsureRequiredTagIsPresentAndValid(DicomTag.SOPClassUID, nameof(DicomTag.StudyInstanceUID));

            // The format of the identifiers will be validated by fo-dicom.
            string studyInstanceUid = EnsureRequiredTagIsPresentAndValid(DicomTag.StudyInstanceUID, nameof(DicomTag.StudyInstanceUID));
            string seriesInstanceUid = EnsureRequiredTagIsPresentAndValid(DicomTag.SeriesInstanceUID, nameof(DicomTag.StudyInstanceUID));
            string sopInstanceUid = EnsureRequiredTagIsPresentAndValid(DicomTag.SOPInstanceUID, nameof(DicomTag.StudyInstanceUID));

            // Ensure the StudyInstanceUid != SeriesInstanceUid != sopInstanceUid
            if (studyInstanceUid == seriesInstanceUid ||
                studyInstanceUid == sopInstanceUid ||
                seriesInstanceUid == sopInstanceUid)
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    DicomCoreResource.DuplicatedUidsNotAllowed);
            }

            // If the requestedStudyInstanceUid is specified, then the StudyInstanceUid must match.
            if (requiredStudyInstanceUid != null &&
                !studyInstanceUid.Equals(requiredStudyInstanceUid, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.MismatchStudyInstanceUid,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.MismatchStudyInstanceUid,
                        studyInstanceUid,
                        requiredStudyInstanceUid));
            }

            string EnsureRequiredTagIsPresentAndValid(DicomTag dicomTag, string parameterName)
            {
                string value = EnsureRequiredTagIsPresent(dicomTag);

                if (!UidValidator.Validate(value))
                {
                    throw new DatasetValidationException(
                       FailureReasonCodes.ValidationFailure,
                       string.Format(
                           CultureInfo.InvariantCulture,
                           DicomCoreResource.InvalidDicomIdentifier,
                           parameterName,
                           value));
                }

                return value;
            }

            string EnsureRequiredTagIsPresent(DicomTag dicomTag)
            {
                if (dicomDataset.TryGetSingleValue(dicomTag, out string value))
                {
                    return value;
                }

                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.MissingRequiredTag,
                        dicomTag.ToString()));
            }
        }
    }
}
