// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.Update;

namespace Microsoft.Health.Dicom.Core.Features.Update;

/// <summary>
/// Provides functionality to validate an <see cref="UpdateInstanceRequest"/>.
/// </summary>
public static class UpdateRequestValidator
{
    /// <summary>
    /// Validates an <see cref="UpdateInstanceRequest"/>.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <exception cref="BadRequestException">Thrown when request body is missing.</exception>
    /// <exception cref="UidValidation">Thrown when the specified StudyInstanceUID is not a valid identifier.</exception>
    public static void ValidateRequest(UpdateInstanceRequest request)
    {
        EnsureArg.IsNotNull(request, nameof(request));
        if (request.UpdateSpec == null)
        {
            throw new BadRequestException(DicomCoreResource.MissingRequestBody);
        }
        else if (request.UpdateSpec.StudyInstanceUids == null)
        {
            throw new BadRequestException(DicomCoreResource.MissingRequiredTag);
        }

        foreach (var StudyInstanceUid in request.UpdateSpec.StudyInstanceUids)
        {
            UidValidation.Validate(StudyInstanceUid, nameof(StudyInstanceUid), allowEmpty: true);
        }
    }

    /// <summary>
    /// Validates an <see cref="DicomDataset"/>.
    /// </summary>
    /// <param name="dataset">The Dicom dataset to validate.</param>
    /// <exception cref="BadRequestException">Thrown when request body is missing.</exception>
    /// <exception cref="UidValidation">Thrown when the specified StudyInstanceUID is not a valid identifier.</exception>
    public static void ValidateDicomDataset(DicomDataset dataset)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));
        foreach (DicomItem item in dataset)
        {
            try
            {
                item.Validate();
            }
            catch (DicomValidationException)
            {
                throw new BadRequestException(DicomCoreResource.DicomElementValidationFailed);
            }
        }
    }
}
