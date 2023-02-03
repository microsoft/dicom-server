// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Diagnostic;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Features.Store;

/// <summary>
/// Provides functionality to validate a <see cref="DicomDataset"/> to make sure it meets the minimum requirement when storing.
/// </summary>
public class StoreDatasetValidator : IStoreDatasetValidator
{
    private readonly bool _enableFullDicomItemValidation;
    private readonly bool _enableDropInvalidDicomJsonMetadata;
    private readonly IElementMinimumValidator _minimumValidator;
    private readonly IQueryTagService _queryTagService;
    private readonly TelemetryClient _telemetryClient;


    public StoreDatasetValidator(
        IOptions<FeatureConfiguration> featureConfiguration,
        IElementMinimumValidator minimumValidator,
        IQueryTagService queryTagService,
        TelemetryClient telemetryClient)
    {
        EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));
        EnsureArg.IsNotNull(minimumValidator, nameof(minimumValidator));
        EnsureArg.IsNotNull(queryTagService, nameof(queryTagService));

        _enableFullDicomItemValidation = featureConfiguration.Value.EnableFullDicomItemValidation;
        _enableDropInvalidDicomJsonMetadata = featureConfiguration.Value.EnableDropInvalidDicomJsonMetadata;
        _minimumValidator = minimumValidator;
        _queryTagService = queryTagService;
        _telemetryClient = EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
    }

    /// <inheritdoc/>
    public async Task<StoreValidationResult> ValidateAsync(
        DicomDataset dicomDataset,
        string requiredStudyInstanceUid,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

        var validationResultBuilder = new StoreValidationResultBuilder();

        try
        {
            ValidateRequiredCoreTags(dicomDataset, requiredStudyInstanceUid);
        }
        catch (DatasetValidationException ex) when (ex.FailureCode == FailureReasonCodes.ValidationFailure)
        {
            validationResultBuilder.Add(ex, ex.DicomTag, isCoreTag: true);
        }

        // validate input data elements
        if (_enableDropInvalidDicomJsonMetadata || _enableFullDicomItemValidation)
        {
            ValidateAllItems(dicomDataset, validationResultBuilder);
        }
        else
        {
            await ValidateIndexedItemsAsync(dicomDataset, validationResultBuilder, cancellationToken);
        }

        // Validate for Implicit VR at the end
        if (ImplicitValueRepresentationValidator.IsImplicitVR(dicomDataset))
        {
            validationResultBuilder.Add(ValidationWarnings.DatasetDoesNotMatchSOPClass);
        }

        return validationResultBuilder.Build();
    }

    private static void ValidateRequiredCoreTags(DicomDataset dicomDataset, string requiredStudyInstanceUid)
    {
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
            var tag = studyInstanceUid == seriesInstanceUid ? DicomTag.SeriesInstanceUID :
                studyInstanceUid == sopInstanceUid ? DicomTag.SOPInstanceUID :
                seriesInstanceUid == sopInstanceUid ? DicomTag.SOPInstanceUID : null;

            throw new DatasetValidationException(
                FailureReasonCodes.ValidationFailure,
                DicomCoreResource.DuplicatedUidsNotAllowed,
                tag);
        }

        // If the requestedStudyInstanceUid is specified, then the StudyInstanceUid must match, ignoring whitespace.
        if (requiredStudyInstanceUid != null &&
            !studyInstanceUid.TrimEnd().Equals(requiredStudyInstanceUid.TrimEnd(), StringComparison.OrdinalIgnoreCase))
        {
            throw new DatasetValidationException(
                FailureReasonCodes.ValidationFailure,
                DicomCoreResource.MismatchStudyInstanceUid,
                DicomTag.StudyInstanceUID);
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
                    dicomTag.ToString()), dicomTag);
        }
    }

    private async Task ValidateIndexedItemsAsync(
        DicomDataset dicomDataset,
        StoreValidationResultBuilder validationResultBuilder,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<QueryTag> queryTags = await _queryTagService.GetQueryTagsAsync(cancellationToken: cancellationToken);

        foreach (QueryTag queryTag in queryTags)
        {
            try
            {
                var validationWarning = dicomDataset.ValidateQueryTag(queryTag, _minimumValidator);

                validationResultBuilder.Add(validationWarning, queryTag.Tag);
            }
            catch (ElementValidationException ex)
            {
                validationResultBuilder.Add(ex, queryTag.Tag);

                _telemetryClient
                    .GetMetric(
                        "IndexTagValidationError",
                        "ExceptionErrorCode",
                        "ExceptionName",
                        "VR")
                    .TrackValue(
                        1,
                        ex.ErrorCode.ToString(),
                        ex.Name,
                        queryTag.VR.Code);
            }
        }
    }

    private void ValidateAllItems(
        DicomDataset dicomDataset,
        StoreValidationResultBuilder validationResultBuilder)
    {
        foreach (DicomItem item in dicomDataset)
        {
            try
            {
                item.Validate();
            }
            catch (DicomValidationException ex)
            {
                if (_enableDropInvalidDicomJsonMetadata)
                {
                    var message = validationResultBuilder.Add(ex, item.Tag);

                    LogForwarder.LogTrace(
                        _telemetryClient,
                        $"{message}. This attribute will be dropped from JSON metadata and can not be used to index. This attribute will remain in your Dicom binary file.",
                        dicomDataset);

                    _telemetryClient
                        .GetMetric(
                            "DroppedInvalidTag",
                            "ExceptionContent",
                            "TagKeyword",
                            "VR",
                            "Tag"
                            )
                        .TrackValue(
                            1,
                            ex.Content,
                            item.Tag.DictionaryEntry.Keyword,
                            item.ValueRepresentation.ToString(),
                            item.Tag.ToString()
                            );
                }
                else
                {
                    validationResultBuilder.Add(ex, item.Tag);
                }
            }
        }
    }
}
