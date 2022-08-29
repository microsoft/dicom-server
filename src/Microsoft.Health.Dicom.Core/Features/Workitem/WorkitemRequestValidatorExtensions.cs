// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.Workitem;

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

internal static class WorkitemRequestValidatorExtensions
{
    /// <summary>
    /// Validates an <see cref="AddWorkitemRequest"/>.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <exception cref="BadRequestException">Thrown when request body is missing.</exception>
    /// <exception cref="UidValidation">Thrown when the specified WorkitemInstanceUID is not a valid identifier.</exception>
    internal static void Validate(this AddWorkitemRequest request)
    {
        EnsureArg.IsNotNull(request, nameof(request));
        if (request.DicomDataset == null)
        {
            throw new BadRequestException(DicomCoreResource.MissingRequestBody);
        }

        UidValidation.Validate(request.WorkitemInstanceUid, nameof(request.WorkitemInstanceUid), allowEmpty: true);
    }

    /// <summary>
    /// Validates an <see cref="UpdateWorkitemRequest"/>.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <exception cref="BadRequestException">Thrown when request body is mising.</exception>
    /// <exception cref="UidValidation">Thrown when the specified WorkitemInstanceUID is not a valid identifier.</exception>
    internal static void Validate(this UpdateWorkitemRequest request)
    {
        EnsureArg.IsNotNull(request, nameof(request));
        if (request.DicomDataset == null)
        {
            throw new BadRequestException(DicomCoreResource.MissingRequestBody);
        }

        UidValidation.Validate(request.WorkitemInstanceUid, nameof(request.WorkitemInstanceUid), allowEmpty: false);

        // Transaction UID can be empty if workitem is in SCHEDULED state.
        UidValidation.Validate(request.TransactionUid, nameof(request.TransactionUid), allowEmpty: true);
    }

    internal static void Validate(this CancelWorkitemRequest request)
    {
        EnsureArg.IsNotNull(request, nameof(request));

        if (request.DicomDataset == null)
        {
            throw new BadRequestException(DicomCoreResource.MissingRequestBody);
        }

        UidValidation.Validate(request.WorkitemInstanceUid, nameof(request.WorkitemInstanceUid), allowEmpty: false);
    }

    internal static void Validate(this ChangeWorkitemStateRequest request)
    {
        EnsureArg.IsNotNull(request, nameof(request));

        if (request.DicomDataset == null)
        {
            throw new BadRequestException(DicomCoreResource.MissingRequestBody);
        }

        UidValidation.Validate(request.WorkitemInstanceUid, nameof(request.WorkitemInstanceUid), allowEmpty: false);
    }

    internal static void Validate(this RetrieveWorkitemRequest request)
    {
        EnsureArg.IsNotNull(request, nameof(request));

        UidValidation.Validate(request.WorkitemInstanceUid, nameof(request.WorkitemInstanceUid), allowEmpty: false);
    }
}
