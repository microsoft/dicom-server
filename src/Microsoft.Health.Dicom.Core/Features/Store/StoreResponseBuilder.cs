// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Store;

/// <summary>
/// Provides functionality to build the response for the store transaction.
/// </summary>
public class StoreResponseBuilder : IStoreResponseBuilder
{
    private readonly IUrlResolver _urlResolver;

    private DicomDataset _dataset;

    private string _message;

    public StoreResponseBuilder(
        IUrlResolver urlResolver
        )
    {
        EnsureArg.IsNotNull(urlResolver, nameof(urlResolver));

        _urlResolver = urlResolver;
    }

    /// <inheritdoc />
    public StoreResponse BuildResponse(string studyInstanceUid)
    {
        bool hasSuccess = _dataset?.TryGetSequence(DicomTag.ReferencedSOPSequence, out _) ?? false;
        bool hasFailure = _dataset?.TryGetSequence(DicomTag.FailedSOPSequence, out _) ?? false;

        StoreResponseStatus status = StoreResponseStatus.None;

        if (hasSuccess && hasFailure)
        {
            // There are both successes and failures.
            status = StoreResponseStatus.PartialSuccess;
        }
        else if (hasSuccess)
        {
            // There are only success.
            status = StoreResponseStatus.Success;
        }
        else if (hasFailure)
        {
            // There are only failures.
            status = StoreResponseStatus.Failure;
        }

        if (hasSuccess && studyInstanceUid != null)
        {
            _dataset.Add(DicomTag.RetrieveURL, _urlResolver.ResolveRetrieveStudyUri(studyInstanceUid).ToString());
        }

        return new StoreResponse(status, _dataset, _message);
    }

    /// <inheritdoc />
    public void AddSuccess(DicomDataset dicomDataset,
        StoreValidationResult storeValidationResult,
        ushort? warningReasonCode = null,
        bool enableDropInvalidDicomJsonMetadata = false)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
        EnsureArg.IsNotNull(storeValidationResult, nameof(storeValidationResult));

        CreateDatasetIfNeeded();

        if (!_dataset.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence referencedSopSequence))
        {
            referencedSopSequence = new DicomSequence(DicomTag.ReferencedSOPSequence);

            _dataset.Add(referencedSopSequence);
        }

        var dicomInstance = dicomDataset.ToInstanceIdentifier();

        var referencedSop = new DicomDataset
        {
            { DicomTag.ReferencedSOPInstanceUID, dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID) },
            { DicomTag.RetrieveURL, _urlResolver.ResolveRetrieveInstanceUri(dicomInstance).ToString() },
            { DicomTag.ReferencedSOPClassUID, dicomDataset.GetFirstValueOrDefault<string>(DicomTag.SOPClassUID) },
        };

        if (!enableDropInvalidDicomJsonMetadata)
        {
            if (warningReasonCode.HasValue)
            {
                referencedSop.Add(DicomTag.WarningReason, warningReasonCode.Value);
            }
        }
        else
        {
            // add comment Sq / list of warnings here
            var warnings = storeValidationResult.InvalidTagErrors.Values.Select(
                    error => new DicomDataset(
                        new DicomLongString(
                            DicomTag.ErrorComment,
                            error.Error)))
                .ToArray();

            var failedSequence = new DicomSequence(
                DicomTag.FailedAttributesSequence,
                warnings);
            referencedSop.Add(failedSequence);
        }

        referencedSopSequence.Items.Add(referencedSop);
    }

    /// <inheritdoc />
    public void AddFailure(DicomDataset dicomDataset, ushort failureReasonCode, StoreValidationResult storeValidationResult = null)
    {
        CreateDatasetIfNeeded();

        if (!_dataset.TryGetSequence(DicomTag.FailedSOPSequence, out DicomSequence failedSopSequence))
        {
            failedSopSequence = new DicomSequence(DicomTag.FailedSOPSequence);

            _dataset.Add(failedSopSequence);
        }

        var failedSop = new DicomDataset()
        {
            { DicomTag.FailureReason, failureReasonCode },
        };

        // We want to turn off auto validation for FailedSOPSequence item
        // because the failure might be caused by invalid UID value.
#pragma warning disable CS0618 // Type or member is obsolete
        failedSop.AutoValidate = false;
#pragma warning restore CS0618 // Type or member is obsolete

        failedSop.AddValueIfNotNull(
            DicomTag.ReferencedSOPInstanceUID,
            dicomDataset?.GetFirstValueOrDefault<string>(DicomTag.SOPInstanceUID));

        failedSop.AddValueIfNotNull(
            DicomTag.ReferencedSOPClassUID,
            dicomDataset?.GetFirstValueOrDefault<string>(DicomTag.SOPClassUID));

        if (storeValidationResult != null)
        {
            var failedAttributes = storeValidationResult.InvalidTagErrors.Values.Select(
                               error => new DicomDataset(
                               new DicomLongString(
                                       DicomTag.ErrorComment,
                                       error.Error))).ToArray();

            var failedAttributeSequence = new DicomSequence(DicomTag.FailedAttributesSequence, failedAttributes);
            failedSop.Add(failedAttributeSequence);
        }

        failedSopSequence.Items.Add(failedSop);
    }

    private void CreateDatasetIfNeeded()
    {
        if (_dataset == null)
        {
            _dataset = new DicomDataset();
        }
    }

    public void SetWarningMessage(string message)
    {
        _message = message;
    }
}
