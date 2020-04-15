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
    /// Provides functionality to validate an <see cref="DicomStoreRequest"/>.
    /// </summary>
    public static class DicomStoreRequestValidator
    {
        /// <summary>
        /// Validates an <see cref="DicomStoreRequest"/>.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <exception cref="DicomBadRequestException">Thrown when request body is missing.</exception>
        /// <exception cref="DicomIdentifierValidator">Thrown when the specified StudyInstanceUID is not a valid identifier.</exception>
        // TODO cleanup this method with Unit tests #72595
        public static void ValidateRequest(DicomStoreRequest request)
        {
            if (request.RequestBody == null)
            {
                throw new DicomBadRequestException("Invalid request");
            }

            if (request.StudyInstanceUid != null)
            {
                DicomIdentifierValidator.ValidateAndThrow(request.StudyInstanceUid, nameof(request.StudyInstanceUid));
            }
        }
    }
}
