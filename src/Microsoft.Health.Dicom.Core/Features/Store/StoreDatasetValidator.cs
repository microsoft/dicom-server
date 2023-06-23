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
        IDicomRequestContextAccessor dicomRequestContextAccessor)
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
        if (_enableFullDicomItemValidation)
        {
            ValidateAllItems(dicomDataset, validationResultBuilder);
        }
        else if (EnableDropMetadata(_dicomRequestContextAccessor.RequestContext.Version))
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
    /// Validate all items with leniency applied.
    /// </summary>
    /// <param name="dicomDataset">Dataset to validate</param>
    /// <param name="validationResultBuilder">Result builder to keep validation results in as validation runs</param>
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
                            ValidateItemWithLeniency(value, de, queryTags);
                        }
                        else
                        {
                            de.Validate();
                        }
                    }
                    catch (DicomValidationException ex)
                    {
                        validationResultBuilder.Add(ex, item.Tag, isCoreTag: RequiredCoreTags.Contains(item.Tag));
                        _storeMeter.V2ValidationError.Add(1, TelemetryDimension(item, IsIndexableTag(queryTags, item)));
                    }
                }
            }
        }
    }

    private void ValidateItemWithLeniency(string value, DicomElement de, IReadOnlyCollection<QueryTag> queryTags)
    {
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
        return queryTags.Any(x => x.Tag == de.Tag);
    }

    private void ValidateWithoutNullPadding(string value, DicomElement de, IReadOnlyCollection<QueryTag> queryTags)
    {
        de.ValueRepresentation.ValidateString(value.TrimEnd('\0'));
        _storeMeter.V2ValidationNullPaddedPassing.Add(
            1,
            TelemetryDimension(de, IsIndexableTag(queryTags, de)));
    }

    private static KeyValuePair<string, object>[] TelemetryDimension(DicomItem item, bool isIndexableTag) =>
        new[]
        {
            new KeyValuePair<string, object>("TagKeyword", item.Tag.DictionaryEntry.Keyword),
            new KeyValuePair<string, object>("VR", item.ValueRepresentation.ToString()),
            new KeyValuePair<string, object>("Tag", item.Tag.ToString()),
            new KeyValuePair<string, object>("IsIndexable", isIndexableTag.ToString())
        };

    private static bool EnableDropMetadata(int? version)
    {
        return version is >= 2;
    }
}
