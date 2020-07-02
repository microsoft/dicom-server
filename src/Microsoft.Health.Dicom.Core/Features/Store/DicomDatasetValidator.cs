// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Query;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to validate a <see cref="DicomDataset"/> to make sure it meets the minimum requirement.
    /// </summary>
    public class DicomDatasetValidator : IDicomDatasetValidator
    {
        private readonly bool _enableFullDicomItemValidation;

        public DicomDatasetValidator(IOptions<FeatureConfiguration> featureConfiguration)
        {
            EnsureArg.IsNotNull(featureConfiguration, nameof(featureConfiguration));

            _enableFullDicomItemValidation = featureConfiguration.Value.EnableFullDicomItemValidation;
        }

        public void Validate(DicomDataset dicomDataset, string requiredStudyInstanceUid)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            // Ensure required tags are present.
            EnsureRequiredTagIsPresent(DicomTag.PatientID);
            EnsureRequiredTagIsPresent(DicomTag.SOPClassUID);

            // The format of the identifiers will be validated by fo-dicom.
            string studyInstanceUid = EnsureRequiredTagIsPresent(DicomTag.StudyInstanceUID);
            string seriesInstanceUid = EnsureRequiredTagIsPresent(DicomTag.SeriesInstanceUID);
            string sopInstanceUid = EnsureRequiredTagIsPresent(DicomTag.SOPInstanceUID);

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

            // validate input data elements
            if (_enableFullDicomItemValidation)
            {
                ValidateAllItems(dicomDataset);
            }
            else
            {
                ValidateIndexedItems(dicomDataset);
            }
        }

        private void ValidateIndexedItems(DicomDataset dicomDataset)
        {
            HashSet<DicomTag> indexableTags = QueryLimit.AllInstancesTags;

            foreach (DicomTag indexableTag in indexableTags)
            {
                try
                {
                    // We can implement our own minimal validators for each VR type if fo-dicom validation is  still more than needed.
                    dicomDataset.GetDicomItem<DicomItem>(indexableTag)?.Validate();
                }
                catch (DicomValidationException ex)
                {
                    throw new DatasetValidationException(
                        FailureReasonCodes.ValidationFailure,
                        ex.Message,
                        ex);
                }
            }
        }

        private void ValidateAllItems(DicomDataset dicomDataset)
        {
            try
            {
                dicomDataset.Each(item => item.Validate());
            }
            catch (DicomValidationException ex)
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    ex.Message,
                    ex);
            }
        }
    }
}
