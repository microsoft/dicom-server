// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Api.Features.Exceptions;
using Microsoft.Health.Dicom.Core.Exceptions;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Exceptions
{
    public class ExceptionHandlingMiddlewareTests
    {
        private readonly DefaultHttpContext _context;

        public ExceptionHandlingMiddlewareTests()
        {
            _context = new DefaultHttpContext();
        }

        [Fact]
        public async Task WhenExecutingExceptionMiddleware_GivenADicomBadRequestException_TheResponseShouldBeBadRequest()
        {
            ExceptionHandlingMiddleware baseExceptionMiddleware = CreateExceptionHandlingMiddleware(innerHttpContext => throw new DicomBadRequestException(string.Empty));

            baseExceptionMiddleware.ExecuteResultAsync(Arg.Any<HttpContext>(), Arg.Any<IActionResult>()).Returns(Task.CompletedTask);

            await baseExceptionMiddleware.Invoke(_context);

            await baseExceptionMiddleware
                .Received()
                .ExecuteResultAsync(
                    Arg.Any<HttpContext>(),
                    Arg.Is<ContentResult>(x => x.StatusCode == (int)HttpStatusCode.BadRequest));
        }

        [Fact]
        public async Task WhenExecutingExceptionMiddleware_GivenAnUnknownException_TheResponseShouldBeInternalServerError()
        {
            ExceptionHandlingMiddleware baseExceptionMiddleware = CreateExceptionHandlingMiddleware(innerHttpContext => throw new Exception());

            baseExceptionMiddleware.ExecuteResultAsync(Arg.Any<HttpContext>(), Arg.Any<IActionResult>()).Returns(Task.CompletedTask);

            await baseExceptionMiddleware.Invoke(_context);

            await baseExceptionMiddleware
                .Received()
                .ExecuteResultAsync(
                    Arg.Any<HttpContext>(),
                    Arg.Is<ContentResult>(x => x.StatusCode == (int)HttpStatusCode.InternalServerError));
        }

        [Fact]
        public async Task WhenExecutingExceptionMiddleware_GivenAnHttpContextWithNoException_TheResponseShouldBeEmpty()
        {
            ExceptionHandlingMiddleware baseExceptionMiddleware = CreateExceptionHandlingMiddleware(innerHttpContext => Task.CompletedTask);

            await baseExceptionMiddleware.Invoke(_context);

            Assert.Equal(200, _context.Response.StatusCode);
            Assert.Null(_context.Response.ContentType);
            Assert.Equal(0, _context.Response.Body.Length);
        }

        private ExceptionHandlingMiddleware CreateExceptionHandlingMiddleware(RequestDelegate nextDelegate)
        {
            return Substitute.ForPartsOf<ExceptionHandlingMiddleware>(nextDelegate, NullLogger<ExceptionHandlingMiddleware>.Instance);
        }
    }
}
