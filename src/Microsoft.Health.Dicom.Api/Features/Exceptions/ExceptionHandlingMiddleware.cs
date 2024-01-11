// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using EnsureThat;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Encryption.Customer.Extensions;
using Microsoft.Health.SqlServer.Features.Storage;
using ComponentModelValidationException = System.ComponentModel.DataAnnotations.ValidationException;
using NotSupportedException = Microsoft.Health.Dicom.Core.Exceptions.NotSupportedException;

namespace Microsoft.Health.Dicom.Api.Features.Exceptions;

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

    private ContentResult MapExceptionToResult(Exception exception)
    {
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
        string message = exception.Message;

        switch (exception)
        {
            case JsonException:
                _logger.LogError(exception, nameof(JsonException));
                message = DicomApiResource.InvalidSyntax;
                statusCode = HttpStatusCode.BadRequest;
                break;
            case ArgumentException:
            case FormatException:
            case InvalidOperationException:
            case ValidationException:
            case ComponentModelValidationException:
            case NotSupportedException:
            case AuditHeaderCountExceededException:
            case AuditHeaderTooLargeException:
            case ConnectionResetException:
            case OperationCanceledException:
            case MicrosoftHealthException ex when IsOperationCanceledException(ex.InnerException):
            case BadHttpRequestException:
            case IOException io when io.Message.Equals("The request stream was aborted.", StringComparison.OrdinalIgnoreCase):
                statusCode = HttpStatusCode.BadRequest;
                break;
            case ConditionalExternalException ex when ex.IsExternal == true:
            case ConditionalExternalException cee when IsCMKException(cee.InnerException):
            case Exception e when IsCMKException(e):
                statusCode = HttpStatusCode.FailedDependency;
                break;
            case ResourceNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                break;
            case NotAcceptableException:
            case TranscodingException:
                statusCode = HttpStatusCode.NotAcceptable;
                break;
            case DicomImageException:
                statusCode = HttpStatusCode.NotAcceptable;
                break;
            case DataStoreException:
                statusCode = HttpStatusCode.ServiceUnavailable;
                break;
            case InstanceAlreadyExistsException:
            case ExtendedQueryTagsAlreadyExistsException:
            case ExtendedQueryTagsOutOfDateException:
            case ExistingOperationException:
                statusCode = HttpStatusCode.Conflict;
                break;
            case PayloadTooLargeException:
                statusCode = HttpStatusCode.RequestEntityTooLarge;
                break;
            case UnsupportedMediaTypeException:
                statusCode = HttpStatusCode.UnsupportedMediaType;
                break;
            case ServiceUnavailableException:
                statusCode = HttpStatusCode.ServiceUnavailable;
                break;
            case ItemNotFoundException:
                // One of the required resources is missing.
                statusCode = HttpStatusCode.InternalServerError;
                break;
            case UnauthorizedDicomActionException udae:
                _logger.LogInformation("Expected data actions not available: {DataActions}", udae.ExpectedDataActions);
                statusCode = HttpStatusCode.Forbidden;
                break;
            case DicomServerException:
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

    private static bool IsOperationCanceledException(Exception ex)
    {
        return ex is OperationCanceledException || (ex is AggregateException aggEx && aggEx.InnerExceptions.Any(x => x is OperationCanceledException));
    }

    private static bool IsCMKException(Exception ex)
    {
        return ex is SqlException sqlEx && sqlEx.IsCMKError() ||
            ex is RequestFailedException rfEx && rfEx.IsCMKError() ||
            (ex is AggregateException aggEx && aggEx.InnerExceptions.Any(x => x is SqlException sqlEx && sqlEx.IsCMKError() || x is RequestFailedException rfEx && rfEx.IsCMKError()));
    }

    private static ContentResult GetContentResult(HttpStatusCode statusCode, string message)
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
