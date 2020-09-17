// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Core.Exceptions;
using NotSupportedException = Microsoft.Health.Dicom.Core.Exceptions.NotSupportedException;

namespace Microsoft.Health.Dicom.Api.Features.Exceptions
{
    public static class ExceptionHandlingHelper
    {
        /// <summary>
        /// Given the exception, get the appropriate HttpStatusCode.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <returns>HttpStatusCode.</returns>
        public static HttpStatusCode GetStatusCode(Exception exception)
        {
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;

            switch (exception)
            {
                case ValidationException _:
                case NotSupportedException _:
                case AuditHeaderCountExceededException _:
                case AuditHeaderTooLargeException _:
                    statusCode = HttpStatusCode.BadRequest;
                    break;
                case ResourceNotFoundException _:
                    statusCode = HttpStatusCode.NotFound;
                    break;
                case NotAcceptableException _:
                case TranscodingException _:
                    statusCode = HttpStatusCode.NotAcceptable;
                    break;
                case DataStoreException _:
                    statusCode = HttpStatusCode.ServiceUnavailable;
                    break;
                case InstanceAlreadyExistsException _:
                    statusCode = HttpStatusCode.Conflict;
                    break;
                case UnsupportedMediaTypeException _:
                    statusCode = HttpStatusCode.UnsupportedMediaType;
                    break;
                case ServiceUnavailableException _:
                    statusCode = HttpStatusCode.ServiceUnavailable;
                    break;
                case ItemNotFoundException _:
                case DicomServerException _:
                    // One of the required resources is missing.
                    statusCode = HttpStatusCode.InternalServerError;
                    break;
            }

            return statusCode;
        }
    }
}
