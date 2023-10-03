// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Features.Exceptions;
using Microsoft.Health.Dicom.Core.Exceptions;
using NSubstitute;
using Xunit;
using NotSupportedException = Microsoft.Health.Dicom.Core.Exceptions.NotSupportedException;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Exceptions;

public class ExceptionHandlingMiddlewareTests
{
    private readonly DefaultHttpContext _context;

    public ExceptionHandlingMiddlewareTests()
    {
        _context = new DefaultHttpContext();
    }

    public static IEnumerable<object[]> GetExceptionToStatusCodeMapping()
    {
        yield return new object[] { new CustomValidationException(), HttpStatusCode.BadRequest };
        yield return new object[] { new ArgumentException(), HttpStatusCode.BadRequest };
        yield return new object[] { new System.ComponentModel.DataAnnotations.ValidationException(), HttpStatusCode.BadRequest };
        yield return new object[] { new NotSupportedException("Not supported."), HttpStatusCode.BadRequest };
        yield return new object[] { new AuditHeaderCountExceededException(AuditConstants.MaximumNumberOfCustomHeaders + 1), HttpStatusCode.BadRequest };
        yield return new object[] { new AuditHeaderTooLargeException("TestHeader", AuditConstants.MaximumLengthOfCustomHeader + 1), HttpStatusCode.BadRequest };
        yield return new object[] { new ResourceNotFoundException("Resource not found."), HttpStatusCode.NotFound };
        yield return new object[] { new TranscodingException(), HttpStatusCode.NotAcceptable };
        yield return new object[] { new DicomImageException(), HttpStatusCode.NotAcceptable };
        yield return new object[] { new DataStoreException(new TaskCanceledException()), HttpStatusCode.BadRequest };
        yield return new object[] { new DataStoreException("Something went wrong."), HttpStatusCode.ServiceUnavailable };
        yield return new object[] { new InstanceAlreadyExistsException(), HttpStatusCode.Conflict };
        yield return new object[] { new UnsupportedMediaTypeException("Media type is not supported."), HttpStatusCode.UnsupportedMediaType };
        yield return new object[] { new ServiceUnavailableException(), HttpStatusCode.ServiceUnavailable };
        yield return new object[] { new ItemNotFoundException(new Exception()), HttpStatusCode.InternalServerError };
        yield return new object[] { new CustomServerException(), HttpStatusCode.ServiceUnavailable };
        yield return new object[] { new BadHttpRequestException("Something bad happened!"), HttpStatusCode.BadRequest };
        yield return new object[] { new IOException("The request stream was aborted."), HttpStatusCode.BadRequest };
        yield return new object[] { new ConnectionResetException(string.Empty), HttpStatusCode.BadRequest };
        yield return new object[] { new OperationCanceledException(), HttpStatusCode.BadRequest };
        yield return new object[] { new TaskCanceledException(), HttpStatusCode.BadRequest };
        yield return new object[] { new InvalidOperationException(), HttpStatusCode.BadRequest };
        yield return new object[] { new PayloadTooLargeException(1), HttpStatusCode.RequestEntityTooLarge };
        yield return new object[] { new DataStoreException(new Exception(), isExternal: true), HttpStatusCode.FailedDependency };
        yield return new object[] { new DataStoreRequestFailedException(new RequestFailedException(String.Empty), isExternal: true), HttpStatusCode.FailedDependency };
    }

    [Theory]
    [MemberData(nameof(GetExceptionToStatusCodeMapping))]
    public async Task GivenAnException_WhenMiddlewareIsExecuted_ThenCorrectStatusCodeShouldBeReturned(Exception exception, HttpStatusCode expectedStatusCode)
    {
        ExceptionHandlingMiddleware baseExceptionMiddleware = CreateExceptionHandlingMiddleware(innerHttpContext => throw exception);

        baseExceptionMiddleware.ExecuteResultAsync(Arg.Any<HttpContext>(), Arg.Any<IActionResult>()).Returns(Task.CompletedTask);

        await baseExceptionMiddleware.Invoke(_context);

        await baseExceptionMiddleware
            .Received()
            .ExecuteResultAsync(
                Arg.Any<HttpContext>(),
                Arg.Is<ContentResult>(x => x.StatusCode == (int)expectedStatusCode));
    }

    [Fact]
    public async Task GivenAnInternalServerException_WhenMiddlewareIsExecuted_ThenMessageShouldBeOverwritten()
    {
        ExceptionHandlingMiddleware baseExceptionMiddleware = CreateExceptionHandlingMiddleware(innerHttpContext => throw new Exception("Unhandled exception."));

        baseExceptionMiddleware.ExecuteResultAsync(Arg.Any<HttpContext>(), Arg.Any<IActionResult>()).Returns(Task.CompletedTask);

        await baseExceptionMiddleware.Invoke(_context);

        await baseExceptionMiddleware
            .Received()
            .ExecuteResultAsync(
                Arg.Any<HttpContext>(),
                Arg.Is<ContentResult>(x => x.Content == DicomApiResource.InternalServerError));
    }

    [Fact]
    public async Task GivenAJsonException_WhenMiddlewareIsExecuted_ThenMessageShouldBeOverwritten()
    {
        ExceptionHandlingMiddleware baseExceptionMiddleware = CreateExceptionHandlingMiddleware(innerHttpContext => throw new JsonException("Parsing data."));

        baseExceptionMiddleware.ExecuteResultAsync(Arg.Any<HttpContext>(), Arg.Any<IActionResult>()).Returns(Task.CompletedTask);

        await baseExceptionMiddleware.Invoke(_context);

        await baseExceptionMiddleware
            .Received()
            .ExecuteResultAsync(
                Arg.Any<HttpContext>(),
                Arg.Is<ContentResult>(x => x.Content == DicomApiResource.InvalidSyntax));
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

    [Fact]
    public async Task GivenAnAggregateExceptionHasTaskCanceled_WhenMiddlewareIsExecuted_ThenMessageShouldBeOverwritten()
    {
        var innerExceptions = new List<Exception>
    {
        new TaskCanceledException("Operation canceled"),
        new ServiceUnavailableException()
    };

        var aggException = new AggregateException(innerExceptions);

        ExceptionHandlingMiddleware baseExceptionMiddleware = CreateExceptionHandlingMiddleware(innerHttpContext => throw new DataStoreException(aggException));

        baseExceptionMiddleware.ExecuteResultAsync(Arg.Any<HttpContext>(), Arg.Any<IActionResult>()).Returns(Task.CompletedTask);

        await baseExceptionMiddleware.Invoke(_context);

        await baseExceptionMiddleware
            .Received()
            .ExecuteResultAsync(
                Arg.Any<HttpContext>(),
                Arg.Is<ContentResult>(x => x.StatusCode.Value == (int)HttpStatusCode.BadRequest));
    }

    private static ExceptionHandlingMiddleware CreateExceptionHandlingMiddleware(RequestDelegate nextDelegate)
    {
        return Substitute.ForPartsOf<ExceptionHandlingMiddleware>(nextDelegate, NullLogger<ExceptionHandlingMiddleware>.Instance);
    }

    private class CustomValidationException : ValidationException
    {
        public CustomValidationException()
            : base("Validation exception.")
        {
        }
    }

    private class CustomServerException : DicomServerException
    {
        public CustomServerException()
            : base("Server exception.")
        {
        }
    }
}
