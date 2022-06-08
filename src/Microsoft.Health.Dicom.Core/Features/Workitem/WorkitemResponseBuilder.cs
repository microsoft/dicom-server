// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.Workitem;
using Microsoft.Health.Dicom.Core.Features.Store;
using System.Linq;

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

/// <summary>
/// Provides functionality to build the response for the store transaction.
/// </summary>
public class WorkitemResponseBuilder : IWorkitemResponseBuilder
{
    private readonly static ushort[] WorkitemConflictFailureReasonCodes = new[]
        {
            FailureReasonCodes.UpsInstanceUpdateNotAllowed,
            FailureReasonCodes.UpsPerformerChoosesNotToCancel,
            FailureReasonCodes.UpsIsAlreadyCanceled,
            FailureReasonCodes.UpsIsAlreadyCompleted
        };

    private readonly IUrlResolver _urlResolver;
    private DicomDataset _dataset;
    private string _message;

    public WorkitemResponseBuilder(IUrlResolver urlResolver)
    {
        EnsureArg.IsNotNull(urlResolver, nameof(urlResolver));

        _urlResolver = urlResolver;
    }

    /// <inheritdoc />
    public AddWorkitemResponse BuildAddResponse()
    {
        Uri url = null;
        WorkitemResponseStatus status = WorkitemResponseStatus.Failure;

        if (!_dataset.TryGetSingleValue<ushort>(DicomTag.FailureReason, out var failureReason))
        {
            status = WorkitemResponseStatus.Success;
            url = _urlResolver.ResolveRetrieveWorkitemUri(_dataset.GetString(DicomTag.SOPInstanceUID));
        }
        else if (failureReason == FailureReasonCodes.SopInstanceAlreadyExists)
        {
            status = WorkitemResponseStatus.Conflict;
        }

        return new AddWorkitemResponse(status, url, _message);
    }

    /// <inheritdoc />
    public CancelWorkitemResponse BuildCancelResponse()
    {
        var status = WorkitemResponseStatus.Failure;

        if (!_dataset.TryGetSingleValue<ushort>(DicomTag.FailureReason, out var failureReason))
        {
            status = WorkitemResponseStatus.Success;
        }
        else if (WorkitemConflictFailureReasonCodes.Contains(failureReason))
        {
            status = WorkitemResponseStatus.Conflict;
        }
        else if (failureReason == FailureReasonCodes.UpsInstanceNotFound)
        {
            status = WorkitemResponseStatus.NotFound;
        }

        return new CancelWorkitemResponse(status, _message);
    }

    public ChangeWorkitemStateResponse BuildChangeWorkitemStateResponse()
    {
        var status = WorkitemResponseStatus.Failure;

        if (!_dataset.TryGetSingleValue<ushort>(DicomTag.FailureReason, out var failureReason))
        {
            status = WorkitemResponseStatus.Success;
        }
        else if (failureReason == FailureReasonCodes.ValidationFailure)
        {
            status = WorkitemResponseStatus.Failure;
        }
        else if (failureReason == FailureReasonCodes.UpsInstanceNotFound)
        {
            status = WorkitemResponseStatus.NotFound;
        }
        else if (failureReason == FailureReasonCodes.UpsInstanceUpdateNotAllowed)
        {
            status = WorkitemResponseStatus.Conflict;
        }

        return new ChangeWorkitemStateResponse(status, _message);
    }

    /// <inheritdoc />
    public RetrieveWorkitemResponse BuildRetrieveWorkitemResponse()
    {
        var status = WorkitemResponseStatus.Failure;

        if (!_dataset.TryGetSingleValue<ushort>(DicomTag.FailureReason, out var failureReason))
        {
            status = WorkitemResponseStatus.Success;
        }
        else if (failureReason == FailureReasonCodes.UpsInstanceNotFound)
        {
            status = WorkitemResponseStatus.NotFound;
        }

        // always remove Transaction UID from the result dicomDataset.
        if (null != _dataset)
        {
            _dataset.Remove(DicomTag.TransactionUID);
        }

        return new RetrieveWorkitemResponse(status, _dataset, _message);
    }

    /// <inheritdoc />
    public UpdateWorkitemResponse BuildUpdateWorkitemResponse(string workitemInstanceUid = null)
    {
        Uri url = null;
        WorkitemResponseStatus status = WorkitemResponseStatus.Failure;

        if (!_dataset.TryGetSingleValue<ushort>(DicomTag.FailureReason, out var failureReason)
            && !string.IsNullOrWhiteSpace(workitemInstanceUid))
        {
            status = WorkitemResponseStatus.Success;
            url = _urlResolver.ResolveRetrieveWorkitemUri(workitemInstanceUid);
        }
        else if (failureReason == FailureReasonCodes.UpsUpdateConflict)
        {
            status = WorkitemResponseStatus.Conflict;
        }

        return new UpdateWorkitemResponse(status, url, _message);
    }

    /// <inheritdoc />
    public void AddSuccess(DicomDataset dicomDataset)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

        _dataset = dicomDataset;
    }

    /// <inheritdoc />
    public void AddSuccess(string warning = default)
    {
        EnsureArg.IsNotNull(warning, nameof(warning));

        _dataset = new DicomDataset();
        _message = warning;
    }

    /// <inheritdoc />
    public void AddFailure(ushort? failureReasonCode, string message = null, DicomDataset dicomDataset = null)
    {
        _message = message;
        _dataset = dicomDataset ?? new DicomDataset();

        _dataset.Add(DicomTag.FailureReason, failureReasonCode.GetValueOrDefault(FailureReasonCodes.ProcessingFailure));
    }
}
