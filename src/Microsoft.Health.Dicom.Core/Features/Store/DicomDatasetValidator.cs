// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
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
        private readonly IQueryTagService _queryTagService;

        public DicomDatasetValidator(IOptions<FeatureConfiguration> featureConfiguration, IDicomElementMinimumValidator minimumValidator, IQueryTagService queryTagService)
        {
            EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));
            EnsureArg.IsNotNull(minimumValidator, nameof(minimumValidator));
            EnsureArg.IsNotNull(queryTagService, nameof(queryTagService));

            _enableFullDicomItemValidation = featureConfiguration.Value.EnableFullDicomItemValidation;
            _minimumValidator = minimumValidator;
            _queryTagService = queryTagService;
        }

        public async Task ValidateAsync(DicomDataset dicomDataset, string requiredStudyInstanceUid, CancellationToken cancellationToken)
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
                await ValidateIndexedItems(dicomDataset, cancellationToken);
            }
        }

        private async Task ValidateIndexedItems(DicomDataset dicomDataset, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<QueryTag> queryTags = await _queryTagService.GetQueryTagsAsync(cancellationToken);
            ValidateTags(dicomDataset, queryTags);
        }

        private void ValidateTags(DicomDataset dicomDataset, IEnumerable<QueryTag> queryTags)
        {
            foreach (QueryTag queryTag in queryTags)
            {
                DicomElement dicomElement = dicomDataset.GetDicomItem<DicomElement>(queryTag.Tag);

                if (dicomElement != null && dicomElement.ValueRepresentation == queryTag.VR)
                {
                    _minimumValidator.Validate(dicomElement);
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
