// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to validate an <see cref="StoreRequest"/>.
    /// </summary>
    public static class StoreRequestValidator
    {
        /// <summary>
        /// Validates an <see cref="StoreRequest"/>.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <exception cref="BadRequestException">Thrown when request body is missing.</exception>
        /// <exception cref="UidValidator">Thrown when the specified StudyInstanceUID is not a valid identifier.</exception>
        // TODO cleanup this method with Unit tests #72595
        public static void ValidateRequest(StoreRequest request)
        {
            if (request.RequestBody == null)
            {
                throw new BadRequestException(DicomCoreResource.MissingRequestBody);
            }

            if (request.StudyInstanceUid != null)
            {
                DicomElementMinimumValidation.ValidateUI(request.StudyInstanceUid, nameof(request.StudyInstanceUid));
            }
        }
    }
}
