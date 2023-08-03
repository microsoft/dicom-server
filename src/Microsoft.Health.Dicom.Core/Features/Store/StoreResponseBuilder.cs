// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
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

    private readonly bool _isPartitionEnabled;

    public StoreResponseBuilder(
        IUrlResolver urlResolver,
        IOptions<FeatureConfiguration> featureConfiguration
    )
    {
        EnsureArg.IsNotNull(urlResolver, nameof(urlResolver));
        EnsureArg.IsNotNull(featureConfiguration, nameof(featureConfiguration));

        _urlResolver = urlResolver;
        _isPartitionEnabled = featureConfiguration.Value.EnableDataPartitions;
    }

    /// <inheritdoc />
    public StoreResponse BuildResponse(string studyInstanceUid, bool returnWarning202 = false)
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
            if (returnWarning202 && HasWarningReasonCode())
            {
                // if we have warning reason code on any of the instances, status code should reflect that
                status = StoreResponseStatus.PartialSuccess;
            }
            else
            {
                // There are only success.
                status = StoreResponseStatus.Success;
            }
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

    private bool HasWarningReasonCode()
    {
        DicomSequence referencedSOPSequence = _dataset.GetSequence(DicomTag.ReferencedSOPSequence);
        return referencedSOPSequence
            .Any(ds => ds.TryGetString(DicomTag.WarningReason, out _) == true);
    }

    /// <inheritdoc />
    public void AddSuccess(DicomDataset dicomDataset,
        StoreValidationResult storeValidationResult,
        Partition partition,
        ushort? warningReasonCode = null,
        bool buildWarningSequence = false)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
        EnsureArg.IsNotNull(storeValidationResult, nameof(storeValidationResult));

        CreateDatasetIfNeeded();

        if (!_dataset.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence referencedSopSequence))
        {
            referencedSopSequence = new DicomSequence(DicomTag.ReferencedSOPSequence);

            _dataset.Add(referencedSopSequence);
        }

        var dicomInstance = dicomDataset.ToInstanceIdentifier(partition);

        var referencedSop = new DicomDataset
        {
            { DicomTag.ReferencedSOPInstanceUID, dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID) },
            { DicomTag.RetrieveURL, _urlResolver.ResolveRetrieveInstanceUri(dicomInstance, _isPartitionEnabled).ToString() },
            { DicomTag.ReferencedSOPClassUID, dicomDataset.GetFirstValueOrDefault<string>(DicomTag.SOPClassUID) },
        };

        if (warningReasonCode.HasValue)
        {
            referencedSop.Add(DicomTag.WarningReason, warningReasonCode.Value);
        }

        if (buildWarningSequence)
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
