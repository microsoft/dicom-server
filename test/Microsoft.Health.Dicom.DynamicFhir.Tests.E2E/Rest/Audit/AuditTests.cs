// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.Health.Fhir.Api.Features.Audit;
using Microsoft.Health.Fhir.Tests.Common.FixtureParameters;
using Microsoft.Health.Fhir.Tests.E2E.Common;
using Xunit;
using FhirClient = Microsoft.Health.Dicom.DynamicFhir.Tests.E2E.Common.FhirClient;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Dicom.DynamicFhir.Tests.E2E.Rest.Audit
{
    [HttpIntegrationFixtureArgumentSets(DataStore.CosmosDb, Format.Json)]
    public class AuditTests : IClassFixture<AuditTestFixture>
    {
        private const string RequestIdHeaderName = "X-Request-Id";
        private const string ExpectedClaimKey = "appid";

        private readonly AuditTestFixture _fixture;
        private readonly FhirClient _client;

        private readonly TraceAuditLogger _auditLogger;

        public AuditTests(AuditTestFixture fixture)
        {
            _fixture = fixture;
            _client = fixture.FhirFixture.FhirClient;
            _auditLogger = _fixture.AuditLogger;
        }

        [Fact]
        public async Task GivenAnExistingResource_WhenRead_ThenAuditLogEntriesShouldBeCreated()
        {
            await ExecuteAndValidate(
                async () =>
                {
                    var studyUid = await _fixture.PostNewSampleStudyAsync();
                    return await _client.ReadAsync<ImagingStudy>(ResourceType.ImagingStudy, studyUid);
                },
                "read",
                ResourceType.ImagingStudy,
                p => $"ImagingStudy/{p.Id}",
                HttpStatusCode.OK);
        }

        [Fact]
        public async Task GivenANonExistingResource_WhenRead_ThenAuditLogEntriesShouldBeCreated()
        {
            // TODO: The resource type being logged here is incorrect. The issue is tracked by https://github.com/Microsoft/fhir-server/issues/334.
            await ExecuteAndValidate(
                async () =>
                {
                    FhirResponse<OperationOutcome> result = null;

                    try
                    {
                        await _client.ReadAsync<Patient>(ResourceType.Patient, "123");
                    }
                    catch (FhirException ex)
                    {
                        result = ex.Response;
                    }

                    return result;
                },
                "read",
                ResourceType.OperationOutcome,
                _ => $"Patient/123",
                HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GivenARequest_WhenNoAuthorizationTokenIsSupplied_ThenAuditLogEntriesShouldBeCreated()
        {
            await ExecuteAndValidate(
                client =>
                {
                    client.HttpClient.DefaultRequestHeaders.Authorization = null;

                    return Task.CompletedTask;
                },
                HttpStatusCode.Unauthorized,
                expectedAppId: null);
        }

        [Fact]
        public async Task GivenARequest_WhenInvalidAuthorizationTokenIsSupplied_ThenAuditLogEntriesShouldBeCreated()
        {
            await ExecuteAndValidate(
                client =>
                {
                    client.HttpClient.SetBearerToken("invalid");

                    return Task.CompletedTask;
                },
                HttpStatusCode.Unauthorized,
                expectedAppId: null);
        }

        [Fact]
        public async Task GivenARequest_WhenValidAuthorizationTokenWithInvalidAudienceIsSupplied_ThenAuditLogEntriesShouldBeCreated()
        {
            await ExecuteAndValidate(
                client => client.RunAsClientApplication(TestApplications.WrongAudienceClient),
                HttpStatusCode.Unauthorized,
                expectedAppId: null);
        }

        [Fact]
        public async Task GivenASmartOnFhirRequest_WhenAuthorizeIsCalled_TheAuditLogEntriesShouldBeCreated()
        {
            const string pathSegment = "AadSmartOnFhirProxy/authorize?client_id=1234&response_type=json&redirect_uri=httptest&aud=localhost";
            await ExecuteAndValidate(
                async () => await _client.HttpClient.GetAsync(pathSegment),
                "smart-on-fhir-authorize",
                pathSegment,
                HttpStatusCode.Redirect,
                "1234",
                "client_id");
        }

        [Fact]
        public async Task GivenASmartOnFhirRequest_WhenCallbackIsCalled_TheAuditLogEntriesShouldBeCreated()
        {
            const string pathSegment = "AadSmartOnFhirProxy/callback/aHR0cHM6Ly9sb2NhbGhvc3Q=?code=1234&state=1234&session_state=1234";
            await ExecuteAndValidate(
                async () => await _client.HttpClient.GetAsync(pathSegment),
                "smart-on-fhir-callback",
                pathSegment,
                HttpStatusCode.BadRequest,
                null,
                null);
        }

        [Fact]
        public async Task GivenASmartOnFhirRequest_WhenTokenIsCalled_TheAuditLogEntriesShouldBeCreated()
        {
            const string pathSegment = "AadSmartOnFhirProxy/token";
            var formFields = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", "1234"),
                new KeyValuePair<string, string>("grant_type", "grantType"),
                new KeyValuePair<string, string>("code", "code"),
                new KeyValuePair<string, string>("redirect_uri", "redirectUri"),
                new KeyValuePair<string, string>("client_secret", "client_secret"),
            };

            var content = new FormUrlEncodedContent(formFields);
            await ExecuteAndValidate(
                async () => await _client.HttpClient.PostAsync(pathSegment, content),
                "smart-on-fhir-token",
                pathSegment,
                HttpStatusCode.BadRequest,
                "1234",
                "client_id");
        }

        [Fact]
        public async Task GivenAResource_WhenNotAuthorized_ThenAuditLogEntriesShouldBeCreated()
        {
            await ExecuteAndValidate(
                client => client.RunAsClientApplication(TestApplications.NativeClient),
                HttpStatusCode.Forbidden,
                expectedAppId: TestApplications.NativeClient.ClientId);
        }

        private async Task ExecuteAndValidate<T>(Func<Task<FhirResponse<T>>> action, string expectedAction, ResourceType expectedResourceType, Func<T, string> expectedPathGenerator, HttpStatusCode expectedStatusCode)
            where T : Resource
        {
            if (!_fixture.FhirFixture.IsUsingInProcTestServer)
            {
                // This test only works with the in-proc server with customized middleware pipeline.
                return;
            }

            FhirResponse<T> response = null;

            response = await action();

            string correlationId = response.Headers.GetValues(RequestIdHeaderName).FirstOrDefault();

            Assert.NotNull(correlationId);

            var expectedUri = new Uri($"http://localhost/{expectedPathGenerator(response.Resource)}");

            string expectedAppId = TestApplications.ServiceClient.ClientId;

            Assert.Collection(
                _auditLogger.GetAuditEntriesByCorrelationId(correlationId),
                ae => ValidateExecutingAuditEntry(ae, expectedAction, expectedUri, correlationId, expectedAppId, ExpectedClaimKey),
                ae => ValidateExecutedAuditEntry(ae, expectedAction, expectedResourceType, expectedUri, expectedStatusCode, correlationId, expectedAppId, ExpectedClaimKey));
        }

        private async Task ExecuteAndValidate(Func<Task<HttpResponseMessage>> action, string expectedAction, string expectedPathSegment, HttpStatusCode expectedStatusCode, string expectedClaimValue, string expectedClaimKey)
        {
            if (!_fixture.FhirFixture.IsUsingInProcTestServer)
            {
                // This test only works with the in-proc server with customized middleware pipeline
                return;
            }

            HttpResponseMessage response = await action();

            string correlationId = response.Headers.GetValues(RequestIdHeaderName).FirstOrDefault();

            Assert.NotNull(correlationId);

            var expectedUri = new Uri($"http://localhost/{expectedPathSegment}");

            Assert.Collection(
                _auditLogger.GetAuditEntriesByCorrelationId(correlationId),
                ae => ValidateExecutingAuditEntry(ae, expectedAction, expectedUri, correlationId, expectedClaimValue, expectedClaimKey),
                ae => ValidateExecutedAuditEntry(ae, expectedAction, null, expectedUri, expectedStatusCode, correlationId, expectedClaimValue, expectedClaimKey));
        }

        private async Task ExecuteAndValidate(Func<FhirClient, Task> clientSetup, HttpStatusCode expectedStatusCode, string expectedAppId)
        {
            if (!_fixture.FhirFixture.IsUsingInProcTestServer || !_fixture.FhirFixture.FhirClient.SecuritySettings.SecurityEnabled)
            {
                // This test only works with the in-proc server with customized middleware pipeline and when security is enabled.
                return;
            }

            const string url = "Patient/123";

            // Create a new client with no token supplied.
            var client = new FhirClient(_fixture.FhirFixture.CreateHttpClient(), ResourceFormat.Json);

            await clientSetup(client);

            FhirResponse<OperationOutcome> response = (await Assert.ThrowsAsync<FhirException>(() => client.ReadAsync<Patient>(url))).Response;

            string correlationId = response.Headers.GetValues(RequestIdHeaderName).FirstOrDefault();

            Assert.NotNull(correlationId);

            var expectedUri = new Uri($"http://localhost/{url}");

            Assert.Collection(
                _auditLogger.GetAuditEntriesByCorrelationId(correlationId),
                ae => ValidateExecutedAuditEntry(ae, "read", ResourceType.Patient, expectedUri, expectedStatusCode, correlationId, expectedAppId, ExpectedClaimKey));
        }

        private void ValidateExecutingAuditEntry(AuditEntry auditEntry, string expectedAction, Uri expectedUri, string expectedCorrelationId, string expectedClaimValue, string expectedClaimKey)
        {
            ValidateAuditEntry(auditEntry, AuditAction.Executing, expectedAction, null, expectedUri, null, expectedCorrelationId, expectedClaimValue, expectedClaimKey);
        }

        private void ValidateExecutedAuditEntry(AuditEntry auditEntry, string expectedAction, ResourceType? expectedResourceType, Uri expectedUri, HttpStatusCode? expectedStatusCode, string expectedCorrelationId, string expectedClaimValue, string expectedClaimKey)
        {
            ValidateAuditEntry(auditEntry, AuditAction.Executed, expectedAction, expectedResourceType, expectedUri, expectedStatusCode, expectedCorrelationId, expectedClaimValue, expectedClaimKey);
        }

        private void ValidateAuditEntry(AuditEntry auditEntry, AuditAction expectedAuditAction, string expectedAction, ResourceType? expectedResourceType, Uri expectedUri, HttpStatusCode? expectedStatusCode, string expectedCorrelationId, string expectedClaimValue, string expectedClaimKey)
        {
            Assert.NotNull(auditEntry);
            Assert.Equal(expectedAuditAction, auditEntry.AuditAction);
            Assert.Equal(expectedAction, auditEntry.Action);
            Assert.Equal(expectedResourceType?.ToString(), auditEntry.ResourceType);
            Assert.Equal(expectedUri, auditEntry.RequestUri);
            Assert.Equal(expectedStatusCode, auditEntry.StatusCode);
            Assert.Equal(expectedCorrelationId, auditEntry.CorrelationId);

            if (expectedClaimValue != null)
            {
                Assert.Equal(1, auditEntry.Claims.Count);
                Assert.Equal(expectedClaimKey, auditEntry.Claims.Single().Key);
                Assert.Equal(expectedClaimValue, auditEntry.Claims.Single().Value);
            }
            else
            {
                Assert.Empty(auditEntry.Claims);
            }
        }
    }
}
