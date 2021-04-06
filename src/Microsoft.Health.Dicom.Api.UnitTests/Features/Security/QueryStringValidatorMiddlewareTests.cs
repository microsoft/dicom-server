// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Dicom.Api.Features.Security;
using Microsoft.Health.Dicom.Core.Exceptions;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Security
{
    public class QueryStringValidatorMiddlewareTests
    {
        private readonly DefaultHttpContext _context;

        public QueryStringValidatorMiddlewareTests()
        {
            _context = new DefaultHttpContext();
        }

        [Theory]
        [InlineData("?<script>alert('test');</script>")]
        [InlineData("?%3Cscript%3Ealert(%27test%27);%3C/script%3E")]
        [InlineData("?%3cscript%3ealert(%27test%27);%3c/script%3e")]
        public async Task WhenExecutingQueryStringValidatorMiddlewareMiddleware_GivenAnInvalidQueryString_TheExceptionShouldBeThrown(string queryString)
        {
            QueryStringValidatorMiddleware queryStringValidatorMiddleware = CreateQueryStringValidatorMiddleware(innerHttpContext => Task.CompletedTask);

            _context.Request.QueryString = new QueryString(queryString);
            await Assert.ThrowsAsync<InvalidQueryStringException>(() => queryStringValidatorMiddleware.Invoke(_context));
        }

        [Fact]
        public async Task WhenExecutingQueryStringValidatorMiddlewareMiddleware_GivenAValidQueryString_TheNoExceptionShouldBeThrown()
        {
            QueryStringValidatorMiddleware queryStringValidatorMiddleware = CreateQueryStringValidatorMiddleware(innerHttpContext => Task.CompletedTask);

            _context.Request.QueryString = new QueryString("?key=value");
            await queryStringValidatorMiddleware.Invoke(_context);

            Assert.Equal(200, _context.Response.StatusCode);
            Assert.Null(_context.Response.ContentType);
        }

        [Fact]
        public async Task WhenExecutingQueryStringValidatorMiddlewareMiddleware_GivenAnEmptyQueryString_TheNoExceptionShouldBeThrown()
        {
            QueryStringValidatorMiddleware queryStringValidatorMiddleware = CreateQueryStringValidatorMiddleware(innerHttpContext => Task.CompletedTask);

            await queryStringValidatorMiddleware.Invoke(_context);

            Assert.Equal(200, _context.Response.StatusCode);
            Assert.Null(_context.Response.ContentType);
        }

        private QueryStringValidatorMiddleware CreateQueryStringValidatorMiddleware(RequestDelegate nextDelegate)
        {
            return Substitute.ForPartsOf<QueryStringValidatorMiddleware>(nextDelegate);
        }
    }
}
