// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    public static class DicomStoreRequestValidator
    {
        // TODO cleanup this method with Unit tests #72595
        public static void ValidateRequest(DicomStoreRequest request)
        {
            if (request.RequestBody == null
                || string.IsNullOrWhiteSpace(request.RequestContentType))
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
