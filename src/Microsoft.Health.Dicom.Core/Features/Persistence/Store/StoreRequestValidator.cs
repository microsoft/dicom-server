// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Core.Features.Persistence.Store
{
    public static class StoreRequestValidator
    {
        private const string MutipartRelated = "multipart/related";

        // TODO cleanup this method with Unit tests #72595
        public static void ValidateRequest(
            Stream requestBody,
            string requestContentType,
            string studyInstanceUid)
        {
            if (requestBody == null ||
                string.IsNullOrWhiteSpace(requestContentType))
            {
                throw new DicomBadRequestException("Invalid request");
            }

            if (studyInstanceUid != null)
            {
                DicomIdentifierValidator.ValidateAndThrow(studyInstanceUid, nameof(studyInstanceUid));
            }

            if (!MediaTypeHeaderValue.TryParse(requestContentType, out MediaTypeHeaderValue media))
            {
                throw new DicomInvalidMediaTypeException();
            }

            var isMultipartRelated = media.MediaType.Equals(MutipartRelated, StringComparison.InvariantCultureIgnoreCase);
            var boundary = HeaderUtilities.RemoveQuotes(media.Boundary).ToString();

            if (!isMultipartRelated || string.IsNullOrWhiteSpace(boundary))
            {
                throw new DicomInvalidMediaTypeException();
            }
        }
    }
}
