// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Core.Exceptions;
using NotSupportedException = Microsoft.Health.Dicom.Core.Exceptions.NotSupportedException;

namespace Microsoft.Health.Dicom.Api.Features.Exceptions
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            EnsureArg.IsNotNull(next, nameof(next));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            ExceptionDispatchInfo exceptionDispatchInfo = null;
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("The response has already started, the base exception middleware will not be executed.");
                    throw;
                }

                // Get the Exception, but don't continue processing in the catch block as its bad for stack usage.
                exceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception);
            }

            if (exceptionDispatchInfo != null)
            {
                IActionResult result = MapExceptionToResult(exceptionDispatchInfo.SourceException);
                await ExecuteResultAsync(context, result);
            }
        }

        private IActionResult MapExceptionToResult(Exception exception)
        {
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            string message = exception.Message;

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
                case ExtendedQueryTagsAlreadyExistsException _:
                case ExtendedQueryTagsOutOfDateException _:
                    statusCode = HttpStatusCode.Conflict;
                    break;
                case UnsupportedMediaTypeException _:
                    statusCode = HttpStatusCode.UnsupportedMediaType;
                    break;
                case ServiceUnavailableException _:
                    statusCode = HttpStatusCode.ServiceUnavailable;
                    break;
                case ItemNotFoundException _:
                    // One of the required resources is missing.
                    statusCode = HttpStatusCode.InternalServerError;
                    break;
                case UnauthorizedDicomActionException udae:
                    _logger.LogInformation("Expected data actions not available: {dataActions}", udae.ExpectedDataActions);
                    statusCode = HttpStatusCode.Forbidden;
                    break;
                case DicomServerException _:
                    statusCode = HttpStatusCode.ServiceUnavailable;
                    break;
            }

            // Log the exception and possibly modify the user message
            switch (statusCode)
            {
                case HttpStatusCode.ServiceUnavailable:
                    _logger.LogWarning(exception, "Service exception.");
                    break;
                case HttpStatusCode.InternalServerError:
                    // In the case of InternalServerError, make sure to overwrite the message to
                    // avoid internal message.
                    _logger.LogCritical(exception, "Unexpected service exception.");
                    message = DicomApiResource.InternalServerError;
                    break;
                default:
                    _logger.LogWarning(exception, "Unhandled exception");
                    break;
            }

            return GetContentResult(statusCode, message);
        }

        private static IActionResult GetContentResult(HttpStatusCode statusCode, string message)
        {
            return new ContentResult
            {
                StatusCode = (int)statusCode,
                Content = message,
            };
        }

        protected internal virtual async Task ExecuteResultAsync(HttpContext context, IActionResult result)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(result, nameof(result));
            await result.ExecuteResultAsync(new ActionContext { HttpContext = context });
        }
    }
}
