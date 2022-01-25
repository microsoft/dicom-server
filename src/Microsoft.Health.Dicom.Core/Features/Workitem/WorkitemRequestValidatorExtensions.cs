// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.WorkitemMessages;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
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
            if (request.RequestBody == null)
            {
                throw new BadRequestException(DicomCoreResource.MissingRequestBody);
            }

            UidValidation.Validate(request.WorkitemInstanceUid, nameof(request.WorkitemInstanceUid), allowEmpty: true);
        }
    }
}
