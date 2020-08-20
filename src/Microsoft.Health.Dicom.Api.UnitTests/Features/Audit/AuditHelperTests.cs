// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Core.Features.Security;
using Microsoft.Health.Dicom.Api.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.Context;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Audit
{
    public class AuditHelperTests
    {
        private const string AuditEventType = "audit";
        private const string CorrelationId = "correlation";
        private static readonly Uri Uri = new Uri("http://localhost/123");
        private static readonly IReadOnlyCollection<KeyValuePair<string, string>> Claims = new List<KeyValuePair<string, string>>();
        private static readonly IPAddress CallerIpAddress = new IPAddress(new byte[] { 0xA, 0x0, 0x0, 0x0 }); // 10.0.0.0
        private const string CallerIpAddressInString = "10.0.0.0";

        private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();
        private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();
        private readonly IAuditHeaderReader _auditHeaderReader = Substitute.For<IAuditHeaderReader>();

        private readonly IDicomRequestContext _dicomRequestContext = Substitute.For<IDicomRequestContext>();

        private readonly IAuditHelper _auditHelper;

        private readonly HttpContext _httpContext = new DefaultHttpContext();
        private readonly IClaimsExtractor _claimsExtractor = Substitute.For<IClaimsExtractor>();

        public AuditHelperTests()
        {
            _dicomRequestContext.Uri.Returns(Uri);
            _dicomRequestContext.CorrelationId.Returns(CorrelationId);

            _dicomRequestContextAccessor.DicomRequestContext = _dicomRequestContext;

            _httpContext.Connection.RemoteIpAddress = CallerIpAddress;

            _claimsExtractor.Extract().Returns(Claims);

            _auditHelper = new AuditHelper(_dicomRequestContextAccessor, _auditLogger, _auditHeaderReader);
        }

        [Fact]
        public void GivenNoAuditEventType_WhenLogExecutingIsCalled_ThenAuditLogShouldNotBeLogged()
        {
            _auditHelper.LogExecuting(_httpContext, _claimsExtractor);

            _auditLogger.DidNotReceiveWithAnyArgs().LogAudit(
                auditAction: default,
                operation: default,
                requestUri: default,
                statusCode: default,
                correlationId: default,
                callerIpAddress: default,
                callerClaims: default);
        }

        [Fact]
        public void GivenAuditEventType_WhenLogExecutingIsCalled_ThenAuditLogShouldBeLogged()
        {
            _dicomRequestContext.AuditEventType.Returns(AuditEventType);

            _auditHelper.LogExecuting(_httpContext, _claimsExtractor);

            _auditLogger.Received(1).LogAudit(
                AuditAction.Executing,
                AuditEventType,
                requestUri: Uri,
                statusCode: null,
                correlationId: CorrelationId,
                callerIpAddress: CallerIpAddressInString,
                callerClaims: Claims,
                customHeaders: _auditHeaderReader.Read(_httpContext));
        }

        [Fact]
        public void GivenNoAuditEventType_WhenLogExecutedIsCalled_ThenAuditLogShouldNotBeLogged()
        {
            _auditHelper.LogExecuted(_httpContext, _claimsExtractor);

            _auditLogger.DidNotReceiveWithAnyArgs().LogAudit(
                auditAction: default,
                operation: default,
                requestUri: default,
                statusCode: default,
                correlationId: default,
                callerIpAddress: default,
                callerClaims: default);
        }

        [Fact]
        public void GivenAuditEventType_WhenLogExecutedIsCalled_ThenAuditLogShouldBeLogged()
        {
            const HttpStatusCode expectedStatusCode = HttpStatusCode.Created;

            _dicomRequestContext.AuditEventType.Returns(AuditEventType);

            _httpContext.Response.StatusCode = (int)expectedStatusCode;

            _auditHelper.LogExecuted(_httpContext, _claimsExtractor);

            _auditLogger.Received(1).LogAudit(
                AuditAction.Executed,
                AuditEventType,
                Uri,
                expectedStatusCode,
                CorrelationId,
                CallerIpAddressInString,
                Claims,
                customHeaders: _auditHeaderReader.Read(_httpContext));
        }
    }
}
