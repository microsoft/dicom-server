// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    public class DatasetCoreTagsValidation : IDatasetValidation
    {
        public DatasetCoreTagsValidation(string requiredStudyInstanceUid)
        {
            RequiredStudyInstanceUid = requiredStudyInstanceUid;
        }

        public string RequiredStudyInstanceUid { get; }

        public void Validate(DicomDataset dataset)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            // Ensure required tags are present.
            EnsureRequiredTagIsPresent(dataset, DicomTag.PatientID);
            EnsureRequiredTagIsPresent(dataset, DicomTag.SOPClassUID);

            // The format of the identifiers will be validated by fo-dicom.
            string studyInstanceUid = EnsureRequiredTagIsPresent(dataset, DicomTag.StudyInstanceUID);
            string seriesInstanceUid = EnsureRequiredTagIsPresent(dataset, DicomTag.SeriesInstanceUID);
            string sopInstanceUid = EnsureRequiredTagIsPresent(dataset, DicomTag.SOPInstanceUID);

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
            if (RequiredStudyInstanceUid != null &&
                !studyInstanceUid.Equals(RequiredStudyInstanceUid, StringComparison.OrdinalIgnoreCase))
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.MismatchStudyInstanceUid,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.MismatchStudyInstanceUid,
                        studyInstanceUid,
                        RequiredStudyInstanceUid));
            }
        }

        private static string EnsureRequiredTagIsPresent(DicomDataset dataset, DicomTag dicomTag)
        {
            if (dataset.TryGetSingleValue(dicomTag, out string value))
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
