// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    private readonly ILogger _logger;

    private static readonly ImmutableHashSet<DicomTag> RequiredCoreTags = ImmutableHashSet.Create(
        DicomTag.StudyInstanceUID,
        DicomTag.SeriesInstanceUID,
        DicomTag.SOPInstanceUID,
        DicomTag.PatientID,
        DicomTag.SOPClassUID);

    private static readonly ImmutableHashSet<DicomTag> RequiredV2CoreTags = ImmutableHashSet.Create(
        DicomTag.StudyInstanceUID,
        DicomTag.SeriesInstanceUID,
        DicomTag.SOPInstanceUID);

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
        bool isV2OrAbove = EnableDropMetadata(_dicomRequestContextAccessor.RequestContext.Version);

        try
        {
            ValidateRequiredCoreTags(dicomDataset, requiredStudyInstanceUid);
            ValidatePatientId(dicomDataset, isV2OrAbove);
        }
        catch (DatasetValidationException ex) when (ex.FailureCode == FailureReasonCodes.ValidationFailure)
        {
            validationResultBuilder.Add(ex, ex.DicomTag, isCoreTag: true);
        }

        // validate input data elements
        if (_enableFullDicomItemValidation)
        {
            ValidateAllItems(dicomDataset, validationResultBuilder);
        }
        else if (isV2OrAbove)
        {
            await ValidateAllItemsWithLeniencyAsync(dicomDataset, validationResultBuilder);
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

    private static void ValidatePatientId(DicomDataset dicomDataset, bool isV2OrAbove)
    {
        // Ensure required tags are present.
        if (!isV2OrAbove)
        {
            EnsureRequiredTagIsPresentWithValue(dicomDataset, DicomTag.PatientID);
        }
        else
        {
            if (!dicomDataset.Contains(DicomTag.PatientID))
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.MissingRequiredTag,
                        DicomTag.PatientID), DicomTag.PatientID);
            }
        }
    }

    private static void ValidateRequiredCoreTags(DicomDataset dicomDataset, string requiredStudyInstanceUid)
    {
        EnsureRequiredTagIsPresentWithValue(dicomDataset, DicomTag.SOPClassUID);

        // The format of the identifiers will be validated by fo-dicom.
        string studyInstanceUid = EnsureRequiredTagIsPresentWithValue(dicomDataset, DicomTag.StudyInstanceUID);
        EnsureRequiredTagIsPresentWithValue(dicomDataset, DicomTag.SeriesInstanceUID);
        EnsureRequiredTagIsPresentWithValue(dicomDataset, DicomTag.SOPInstanceUID);

        // If the requestedStudyInstanceUid is specified, then the StudyInstanceUid must match, ignoring whitespace.
        if (requiredStudyInstanceUid != null &&
            !studyInstanceUid.TrimEnd().Equals(requiredStudyInstanceUid.TrimEnd(), StringComparison.OrdinalIgnoreCase))
        {
            throw new DatasetValidationException(
                FailureReasonCodes.ValidationFailure,
                DicomCoreResource.MismatchStudyInstanceUid,
                DicomTag.StudyInstanceUID);
        }
    }

    private static string EnsureRequiredTagIsPresentWithValue(DicomDataset dicomDataset, DicomTag dicomTag)
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

    private static void ValidateAllItems(
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
                validationResultBuilder.Add(ex, item.Tag);
            }
        }
    }

    /// <summary>
    /// Validate all items. Generate errors for core tags with leniency applied and generate warnings for all of DS items.
    /// </summary>
    /// <param name="dicomDataset">Dataset to validate</param>
    /// <param name="validationResultBuilder">Result builder to keep errors and warnings in as validation runs</param>
    /// <remarks>
    /// We only need to validate SQ and DicomElement types. The only other type under DicomItem
    /// is DicomFragmentSequence, which does not implement validation and can be skipped.
    /// An example of a type of DicomFragmentSequence is DicomOtherByteFragment.
    /// See https://fo-dicom.github.io/stable/v5/api/FellowOakDicom.DicomItem.html
    /// </remarks>
    private async Task ValidateAllItemsWithLeniencyAsync(
        DicomDataset dicomDataset,
        StoreValidationResultBuilder validationResultBuilder)
    {
        IReadOnlyCollection<QueryTag> queryTags = await _queryTagService.GetQueryTagsAsync();

        GenerateErrors(dicomDataset, validationResultBuilder, queryTags);

        GenerateValidationWarnings(dicomDataset, validationResultBuilder, queryTags);
    }

    /// <summary>
    ///  Generate errors on result by validating each core tag in dataset
    /// </summary>
    /// <remarks>
    /// isCoreTag is utilized in store service when building a response and to know whether or not to create a failure or
    /// success response. Anything added here results in a 409 conflict with errors in body.
    /// </remarks>
    /// <param name="dicomDataset"></param>
    /// <param name="validationResultBuilder"></param>
    /// <param name="queryTags">Queryable dicom tags</param>
    private void GenerateErrors(DicomDataset dicomDataset, StoreValidationResultBuilder validationResultBuilder,
        IReadOnlyCollection<QueryTag> queryTags)
    {
        foreach (var requiredCoreTag in RequiredCoreTags)
        {
            try
            {
                // validate with additional leniency
                var validationWarning =
                    dicomDataset.ValidateDicomTag(requiredCoreTag, _minimumValidator, validationLevel: ValidationLevel.Default);

                validationResultBuilder.Add(validationWarning, requiredCoreTag);
            }
            catch (ElementValidationException ex)
            {
                validationResultBuilder.Add(ex, requiredCoreTag, isCoreTag: true);
                _logger.LogInformation("Dicom instance validation failed with error on required core tag {Tag} with id {Id}", requiredCoreTag.DictionaryEntry.Keyword, requiredCoreTag);
                _storeMeter.V2ValidationError.Add(1,
                    TelemetryDimension(requiredCoreTag, requiredCoreTag.GetDefaultVR(), IsIndexableTag(queryTags, requiredCoreTag)));
            }
        }
    }

    /// <summary>
    /// Generate warnings on the results by validating each item in dataset 
    /// </summary>
    /// <remarks>
    /// isCoreTag is utilized in store service when building a response and to know whether or not to create a failure or
    /// success response. Anything added here results in a 202 Accepted with warnings in body.
    /// Since we are providing warnings as informational value, we want to use full fo-dicom validation and no leniency
    /// or our minimum validators.
    /// We also need to iterate through each item at a time to capture all validation issues for each and all items in
    /// the dataset instead of just excepting at first issue as fo-dicom does.
    /// Do not produce a warning when string invalid only due to null padding. 
    /// </remarks>
    private void GenerateValidationWarnings(DicomDataset dicomDataset, StoreValidationResultBuilder validationResultBuilder,
        IReadOnlyCollection<QueryTag> queryTags)
    {
        var stack = new Stack<DicomDataset>();
        stack.Push(dicomDataset);
        while (stack.Count > 0)
        {
            DicomDataset ds = stack.Pop();

            // add to stack to keep iterating when SQ type, otherwise validate
            foreach (DicomItem item in ds)
            {
                if (item is DicomSequence sequence)
                {
                    foreach (DicomDataset childDs in sequence)
                    {
                        stack.Push(childDs);
                    }
                }
                else if (item is DicomElement de)
                {
                    try
                    {
                        if (de.ValueRepresentation.ValueType == typeof(string))
                        {
                            string value = ds.GetString(de.Tag);
                            ValidateStringItemWithLeniency(value, de, queryTags);
                        }
                        else
                        {
                            de.Validate();
                        }
                    }
                    catch (Exception ex) when (ex is DicomValidationException || ex is DatasetValidationException)
                    {
                        validationResultBuilder.Add(ex, item.Tag, isCoreTag: false);
                        if (item.Tag.IsPrivate)
                        {
                            _logger.LogInformation("Dicom instance validation succeeded but with warning on a private tag");
                        }
                        else
                        {
                            _logger.LogInformation("Dicom instance validation succeeded but with warning on non-private tag {Tag} with id {Id}, where tag is indexable: {Indexable} and VR is {VR}", item.Tag.DictionaryEntry.Keyword, item.Tag, IsIndexableTag(queryTags, item), item.ValueRepresentation);
                        }
                        _storeMeter.V2ValidationError.Add(1, TelemetryDimension(item, IsIndexableTag(queryTags, item)));
                    }
                }
            }
        }
    }

    private void ValidateStringItemWithLeniency(string value, DicomElement de, IReadOnlyCollection<QueryTag> queryTags)
    {
        if (de.Tag == DicomTag.PatientID && string.IsNullOrWhiteSpace(value))
        {
            throw new DatasetValidationException(
                FailureReasonCodes.ValidationFailure,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DicomCoreResource.MissingRequiredTag,
                    DicomTag.PatientID), DicomTag.PatientID);
        }

        if (value != null && value.EndsWith('\0'))
        {
            ValidateWithoutNullPadding(value, de, queryTags);
        }
        else
        {
            de.Validate();
        }
    }

    private static bool IsIndexableTag(IReadOnlyCollection<QueryTag> queryTags, DicomItem de)
    {
        return IsIndexableTag(queryTags, de.Tag);
    }

    private static bool IsIndexableTag(IReadOnlyCollection<QueryTag> queryTags, DicomTag tag)
    {
        return queryTags.Any(x => x.Tag == tag);
    }

    /// <summary>
    /// Check if a tag is a required core tag
    /// </summary>
    /// <param name="tag">tag to check if it is required</param>
    /// <returns>whether or not tag is required</returns>
    public static bool IsV2CoreTag(DicomTag tag)
    {
        return RequiredV2CoreTags.Contains(tag);
    }

    private void ValidateWithoutNullPadding(string value, DicomElement de, IReadOnlyCollection<QueryTag> queryTags)
    {
        de.ValueRepresentation.ValidateString(value.TrimEnd('\0'));
        _storeMeter.V2ValidationNullPaddedPassing.Add(
            1,
            TelemetryDimension(de, IsIndexableTag(queryTags, de)));
    }

    private static KeyValuePair<string, object>[] TelemetryDimension(DicomItem item, bool isIndexableTag) =>
        TelemetryDimension(item.Tag, item.ValueRepresentation, isIndexableTag);

    private static KeyValuePair<string, object>[] TelemetryDimension(DicomTag tag, DicomVR vr, bool isIndexableTag) =>
        new[]
        {
            new KeyValuePair<string, object>("TagKeyword", tag?.DictionaryEntry?.Keyword),
            new KeyValuePair<string, object>("VR", vr?.ToString()),
            new KeyValuePair<string, object>("Tag", tag?.ToString()),
            new KeyValuePair<string, object>("IsIndexable", isIndexableTag.ToString())
        };

    private static bool EnableDropMetadata(int? version)
    {
        return version is >= 2;
    }
}
