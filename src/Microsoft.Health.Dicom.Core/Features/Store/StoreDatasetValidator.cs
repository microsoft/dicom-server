// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Features.Store;

/// <summary>
/// Provides functionality to validate a <see cref="DicomDataset"/> to make sure it meets the minimum requirement when storing.
/// </summary>
public class StoreDatasetValidator : IStoreDatasetValidator
{
    private readonly bool _enableFullDicomItemValidation;
    private readonly IElementMinimumValidator _minimumValidator;
    private readonly IQueryTagService _queryTagService;
    private readonly StoreMeter _storeMeter;
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
    private readonly ILogger<StoreDatasetValidator> _logger;

    private static readonly HashSet<DicomTag> RequiredCoreTags = new HashSet<DicomTag>()
    {
        DicomTag.StudyInstanceUID,
        DicomTag.SeriesInstanceUID,
        DicomTag.SOPInstanceUID,
        DicomTag.PatientID,
        DicomTag.SOPClassUID,
    };

    public StoreDatasetValidator(
        IOptions<FeatureConfiguration> featureConfiguration,
        IElementMinimumValidator minimumValidator,
        IQueryTagService queryTagService,
        StoreMeter storeMeter,
        IDicomRequestContextAccessor dicomRequestContextAccessor,
        ILogger<StoreDatasetValidator> logger)
    {
        EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));
        EnsureArg.IsNotNull(minimumValidator, nameof(minimumValidator));
        EnsureArg.IsNotNull(queryTagService, nameof(queryTagService));
        EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));

        _dicomRequestContextAccessor = dicomRequestContextAccessor;
        _enableFullDicomItemValidation = featureConfiguration.Value.EnableFullDicomItemValidation;
        _minimumValidator = minimumValidator;
        _queryTagService = queryTagService;
        _storeMeter = EnsureArg.IsNotNull(storeMeter, nameof(storeMeter));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
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
        if (EnableDropMetadata(_dicomRequestContextAccessor.RequestContext.Version) || _enableFullDicomItemValidation)
        {
            await ValidateAllItems(dicomDataset, validationResultBuilder);
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
                _storeMeter.IndexTagValidationError.Add(
                    1,
                    new[]
                    {
                        new KeyValuePair<string, object>("ExceptionErrorCode", ex.ErrorCode.ToString()),
                        new KeyValuePair<string, object>("ExceptionName", ex.Name),
                        new KeyValuePair<string, object>("VR", queryTag.VR.Code)
                    });
            }
        }
    }

    private async Task ValidateAllItems(
        DicomDataset dicomDataset,
        StoreValidationResultBuilder validationResultBuilder)
    {
        IReadOnlyCollection<QueryTag> queryTags = await _queryTagService.GetQueryTagsAsync();
        var indexableTags = queryTags.Select(t => t.Tag).ToList();
        foreach (DicomItem item in dicomDataset)
        {
            try
            {
                item.Validate();
            }
            catch (DicomValidationException ex)
            {
                if (EnableDropMetadata(_dicomRequestContextAccessor.RequestContext.Version))
                {
                    if (RequiredCoreTags.Contains(item.Tag))
                    {
                        validationResultBuilder.Add(ex, item.Tag, isCoreTag: true);
                    }
                    else
                    {
                        validationResultBuilder.Add(ex, item.Tag);
                    }

                    bool isIndexableTag = indexableTags.Contains(item.Tag);
                    _storeMeter.ValidateAllValidationError.Add(
                        1,
                        new[]
                        {
                            new KeyValuePair<string, object>("TagKeyword", item.Tag.DictionaryEntry.Keyword),
                            new KeyValuePair<string, object>("VR", item.ValueRepresentation.ToString()),
                            new KeyValuePair<string, object>("Tag", item.Tag.ToString()),
                            new KeyValuePair<string, object>("IsIndexable", isIndexableTag.ToString())
                        });

                    _logger.LogInformation(
                        "Validation error for tag {Tag} {TagKeyword} with VR {VR}, IsIndexable: {IsIndexable}. Content: {Content}.",
                        item.Tag.ToString(),
                        item.Tag.DictionaryEntry.Keyword,
                        item.ValueRepresentation.ToString(),
                        ex.Content,
                        isIndexableTag.ToString());
                }
                else
                {
                    validationResultBuilder.Add(ex, item.Tag);
                }
            }
        }
    }

    private static bool EnableDropMetadata(int? version)
    {
        return version is >= 2;
    }
}
