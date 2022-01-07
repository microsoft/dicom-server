// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to validate a <see cref="DicomDataset"/> to make sure it meets the minimum requirement when Adding.
    /// </summary>
    public class AddWorkitemDatasetValidator : IAddWorkitemDatasetValidator
    {
        private readonly bool _enableFullDicomItemValidation;
        private readonly IElementMinimumValidator _minimumValidator;
        private readonly IQueryTagService _queryTagService;

        public AddWorkitemDatasetValidator(IOptions<FeatureConfiguration> featureConfiguration, IElementMinimumValidator minimumValidator, IQueryTagService queryTagService)
        {
            EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));
            EnsureArg.IsNotNull(minimumValidator, nameof(minimumValidator));
            EnsureArg.IsNotNull(queryTagService, nameof(queryTagService));

            _enableFullDicomItemValidation = featureConfiguration.Value.EnableFullDicomItemValidation;
            _minimumValidator = minimumValidator;
            _queryTagService = queryTagService;
        }

        public void Validate(DicomDataset dicomDataset, string workitemInstanceUid)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            ValidateRequiredTags(dicomDataset, workitemInstanceUid);

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

        private static void ValidateRequiredTags(DicomDataset dicomDataset, string workitemInstanceUid)
        {
            // Ensure required tags are present.
            EnsureRequiredTagIsPresent(DicomTag.ScheduledProcedureStepPriority);
            EnsureRequiredTagIsPresent(DicomTag.ProcedureStepLabel);
            EnsureRequiredTagIsPresent(DicomTag.WorklistLabel);
            EnsureRequiredTagIsPresent(DicomTag.ScheduledStationNameCodeSequence);
            EnsureRequiredTagIsPresent(DicomTag.ScheduledStationClassCodeSequence);
            EnsureRequiredTagIsPresent(DicomTag.ScheduledStationGeographicLocationCodeSequence);
            EnsureRequiredTagIsPresent(DicomTag.ScheduledHumanPerformersSequence);
            EnsureRequiredTagIsPresent(DicomTag.HumanPerformerCodeSequence);
            EnsureRequiredTagIsPresent(DicomTag.ScheduledProcedureStepStartDateTime);
            EnsureRequiredTagIsPresent(DicomTag.ExpectedCompletionDateTime);
            EnsureRequiredTagIsPresent(DicomTag.ScheduledWorkitemCodeSequence);
            EnsureRequiredTagIsPresent(DicomTag.InputReadinessState);
            EnsureRequiredTagIsPresent(DicomTag.PatientName);
            EnsureRequiredTagIsPresent(DicomTag.PatientID);
            EnsureRequiredTagIsPresent(DicomTag.PatientBirthDate);
            EnsureRequiredTagIsPresent(DicomTag.PatientSex);
            EnsureRequiredTagIsPresent(DicomTag.AdmissionID);
            EnsureRequiredTagIsPresent(DicomTag.IssuerOfAdmissionIDSequence);
            EnsureRequiredTagIsPresent(DicomTag.ReferencedRequestSequence);
            EnsureRequiredTagIsPresent(DicomTag.AccessionNumber);
            EnsureRequiredTagIsPresent(DicomTag.IssuerOfAccessionNumberSequence);
            EnsureRequiredTagIsPresent(DicomTag.RequestedProcedureID);
            EnsureRequiredTagIsPresent(DicomTag.RequestingService);
            EnsureRequiredTagIsPresent(DicomTag.ReplacedProcedureStepSequence);
            EnsureRequiredTagIsPresent(DicomTag.ProcedureStepState);

            // The format of the identifiers will be validated by fo-dicom.
            string workitemUid = EnsureRequiredTagIsPresent(DicomTag.AffectedSOPInstanceUID);

            // If the workitemInstanceUid is specified, then the workitemUid must match.
            if (workitemInstanceUid != null &&
                !workitemUid.Equals(workitemInstanceUid, StringComparison.OrdinalIgnoreCase))
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.MismatchWorkitemInstanceUid,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.MismatchWorkitemInstanceUid,
                        workitemUid,
                        workitemInstanceUid));
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

        private void ValidateIndexedItems(DicomDataset dicomDataset)
        {
            IReadOnlyCollection<QueryTag> queryTags = QueryLimit.IndexedWorkItemQueryTags.Select(x => new QueryTag(x)).ToList();
            foreach (QueryTag queryTag in queryTags)
            {
                dicomDataset.ValidateQueryTag(queryTag, _minimumValidator);
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
