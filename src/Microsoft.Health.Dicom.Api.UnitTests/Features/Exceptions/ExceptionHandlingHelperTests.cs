// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Features.Exceptions;
using Microsoft.Health.Dicom.Core.Exceptions;
using Xunit;
using NotSupportedException = Microsoft.Health.Dicom.Core.Exceptions.NotSupportedException;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Exceptions
{
    public class ExceptionHandlingHelperTests
    {
        public static IEnumerable<object[]> GetExceptionToStatusCodeMapping()
        {
            yield return new object[] { new CustomValidationException(), HttpStatusCode.BadRequest };
            yield return new object[] { new NotSupportedException("Not supported."), HttpStatusCode.BadRequest };
            yield return new object[] { new AuditHeaderCountExceededException(AuditConstants.MaximumNumberOfCustomHeaders + 1), HttpStatusCode.BadRequest };
            yield return new object[] { new AuditHeaderTooLargeException("TestHeader", AuditConstants.MaximumLengthOfCustomHeader + 1), HttpStatusCode.BadRequest };
            yield return new object[] { new ResourceNotFoundException("Resource not found."), HttpStatusCode.NotFound };
            yield return new object[] { new TranscodingException(), HttpStatusCode.NotAcceptable };
            yield return new object[] { new DataStoreException("Something went wrong."), HttpStatusCode.ServiceUnavailable };
            yield return new object[] { new InstanceAlreadyExistsException(), HttpStatusCode.Conflict };
            yield return new object[] { new UnsupportedMediaTypeException("Media type is not supported."), HttpStatusCode.UnsupportedMediaType };
            yield return new object[] { new ServiceUnavailableException(), HttpStatusCode.ServiceUnavailable };
            yield return new object[] { new ItemNotFoundException(new Exception()), HttpStatusCode.InternalServerError };
            yield return new object[] { new CustomServerException(), HttpStatusCode.InternalServerError };
        }

        [Theory]
        [MemberData(nameof(GetExceptionToStatusCodeMapping))]
        public void GivenAnException_WhenGetStatusCodeIsCalled_ThenCorrectStatusCodeShouldBeReturned(Exception exception, HttpStatusCode expectedStatusCode)
        {
            HttpStatusCode actualStatusCode = ExceptionHandlingHelper.GetStatusCode(exception);

            Assert.Equal(expectedStatusCode, actualStatusCode);
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
}
