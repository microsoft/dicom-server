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
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to validate a <see cref="DicomDataset"/> to make sure it meets the minimum requirement.
    /// </summary>
    public class DicomDatasetValidator : IDicomDatasetValidator
    {
        private readonly bool _enableFullDicomItemValidation;
        private readonly IDicomElementMinimumValidator _minimumValidator;

        public DicomDatasetValidator(IOptions<FeatureConfiguration> featureConfiguration, IDicomElementMinimumValidator minimumValidator)
        {
            EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));
            EnsureArg.IsNotNull(minimumValidator, nameof(minimumValidator));

            _enableFullDicomItemValidation = featureConfiguration.Value.EnableFullDicomItemValidation;
            _minimumValidator = minimumValidator;
        }

        public void Validate(DicomDataset dicomDataset, IReadOnlyList<CustomTagEntry> customTagEntries, string requiredStudyInstanceUid)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(customTagEntries, nameof(customTagEntries));

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
                !studyInstanceUid.Equals(requiredStudyInstanceUid, StringComparison.OrdinalIgnoreCase))
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
                ValidateIndexedItems(dicomDataset, customTagEntries);
            }
        }

        private void ValidateIndexedItems(DicomDataset dicomDataset, IReadOnlyList<CustomTagEntry> customTagEntries)
        {
            HashSet<DicomTag> standardTags = new HashSet<DicomTag>();
            Dictionary<string, CustomTagEntry> privateTags = new Dictionary<string, CustomTagEntry>();
            foreach (var customTagEntry in customTagEntries)
            {
                DicomTag dicomTag = DicomTag.Parse(customTagEntry.Path);
                if (dicomTag.IsPrivate)
                {
                    privateTags.Add(customTagEntry.Path, customTagEntry);
                }
                else
                {
                    standardTags.Add(dicomTag);
                }
            }

            // Process Standard Tag
            HashSet<DicomTag> indexableTags = QueryLimit.AllInstancesTags;
            indexableTags.UnionWith(standardTags);

            foreach (DicomTag indexableTag in indexableTags)
            {
                DicomElement dicomElement = dicomDataset.GetDicomItem<DicomElement>(indexableTag);

                if (dicomElement != null)
                {
                    _minimumValidator.Validate(dicomElement);
                }
            }

            // Process Private Tag
            // dicomDataset.GetDicomItem<DicomElement>() cannot get value for private tag, we need to loop and compare with path.
            foreach (DicomItem item in dicomDataset)
            {
                if (item.Tag.IsPrivate)
                {
                    string path = item.Tag.GetPath();
                    if (privateTags.ContainsKey(path) && privateTags[path].VR.Equals(item.ValueRepresentation.Code, StringComparison.Ordinal))
                    {
                        DicomElement element = item as DicomElement;
                        if (element != null)
                        {
                            _minimumValidator.Validate(element);
                        }
                    }
                }
            }
        }

        private static void ValidateAllItems(DicomDataset dicomDataset)
        {
            dicomDataset.Each(item =>
            {
                item.ValidateDicomItem();
            });
        }
    }
}
