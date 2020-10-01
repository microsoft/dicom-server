// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Core.Features.Security;
using Microsoft.Health.Dicom.Api.Features.Audit;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Audit
{
    public class AuditMiddlewareTests
    {
        private readonly IClaimsExtractor _claimsExtractor = Substitute.For<IClaimsExtractor>();
        private readonly IAuditHelper _auditHelper = Substitute.For<IAuditHelper>();
        private readonly IAuditEgressLogger _auditEgressLogger;

        private readonly AuditMiddleware _auditMiddleware;

        private readonly HttpContext _httpContext = new DefaultHttpContext();

        public AuditMiddlewareTests()
        {
            _auditEgressLogger = new AuditEgressLogger();

            _auditMiddleware = new AuditMiddleware(
                httpContext => Task.CompletedTask,
                _claimsExtractor,
                _auditHelper,
                _auditEgressLogger);
        }

        [Fact]
        public async Task GivenNotAuthXFailure_WhenInvoked_ThenAuditLogShouldBeLogged()
        {
            _httpContext.Response.StatusCode = (int)HttpStatusCode.OK;

            await _auditMiddleware.Invoke(_httpContext);

            _auditHelper.Received(1).LogExecuted(_httpContext, _claimsExtractor);
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        public async Task GivenAuthXFailure_WhenInvoked_ThenAuditLogShouldBeLogged(HttpStatusCode statusCode)
        {
            _httpContext.Response.StatusCode = (int)statusCode;

            await _auditMiddleware.Invoke(_httpContext);

            _auditHelper.Received(1).LogExecuted(_httpContext, _claimsExtractor);
        }
    }
}
