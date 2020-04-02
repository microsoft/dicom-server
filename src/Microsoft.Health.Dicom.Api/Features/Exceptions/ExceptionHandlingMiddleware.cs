// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;

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

                var result = MapExceptionToResult(exception);
                await ExecuteResultAsync(context, result);
            }
        }

        private IActionResult MapExceptionToResult(Exception exception)
        {
            switch (exception)
            {
                case DicomValidationException _:
                    return GetResult(HttpStatusCode.BadRequest, exception.Message);
                case DicomServerException _:
                    return GetResult(HttpStatusCode.ServiceUnavailable, exception.Message);

                // TODO remove below exception after we clean up all exceptions
                case DicomException dicomException:
                    return GetResult(dicomException.ResponseStatusCode, dicomException.Message);
                default:
                    _logger.LogError("Unhandled exception: {0}", exception);
                    return GetResult(HttpStatusCode.InternalServerError);
            }
        }

        private IActionResult GetResult(HttpStatusCode statusCode, string message)
        {
            return new ContentResult
            {
                StatusCode = (int)statusCode,
                Content = message,
            };
        }

        private IActionResult GetResult(HttpStatusCode statusCode)
        {
            return new ContentResult
            {
                StatusCode = (int)statusCode,
            };
        }

        protected internal virtual async Task ExecuteResultAsync(HttpContext context, IActionResult result)
        {
            await result.ExecuteResultAsync(new ActionContext { HttpContext = context });
        }
    }
}
