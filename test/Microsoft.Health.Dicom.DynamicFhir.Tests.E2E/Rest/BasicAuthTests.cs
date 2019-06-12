// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Net.Http;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Tests.Common;
using Microsoft.Health.Fhir.Tests.Common.FixtureParameters;
using Microsoft.Health.Fhir.Tests.E2E.Common;
using Xunit;
using FhirClient = Microsoft.Health.Dicom.DynamicFhir.Tests.E2E.Common.FhirClient;
using FhirStartup = Microsoft.Health.Dicom.DynamicFhir.Web.Startup;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Dicom.DynamicFhir.Tests.E2E.Rest
{
    /// <summary>
    /// NOTE: These tests will fail if security is disabled..
    /// </summary>
    [Trait(Traits.Category, Categories.Authorization)]
    [HttpIntegrationFixtureArgumentSets(DataStore.CosmosDb, Format.Json)]
    public class BasicAuthTests : IClassFixture<DicomFhirIntegrationTestFixture<FhirStartup>>
    {
        private const string ForbiddenMessage = "Forbidden: Authorization failed.";
        private const string UnauthorizedMessage = "Unauthorized: Authentication failed.";
        private const string Invalidtoken = "eyJhbGciOiJSUzI1NiIsImtpZCI6ImNmNWRmMGExNzY5ZWIzZTFkOGRiNWIxMGZiOWY3ZTk0IiwidHlwIjoiSldUIn0.eyJuYmYiOjE1NDQ2ODQ1NzEsImV4cCI6MTU0NDY4ODE3MSwiaXNzIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6NDQzNDgiLCJhdWQiOlsiaHR0cHM6Ly9sb2NhbGhvc3Q6NDQzNDgvcmVzb3VyY2VzIiwiZmhpci1haSJdLCJjbGllbnRfaWQiOiJzZXJ2aWNlY2xpZW50Iiwicm9sZXMiOiJhZG1pbiIsImFwcGlkIjoic2VydmljZWNsaWVudCIsInNjb3BlIjpbImZoaXItYWkiXX0.SKSvy6Jxzwsv1ZSi0PO4Pdq6QDZ6mBJIRxUPgoPlz2JpiB6GMXu5u0n1IpS6zOXihGkGhegjtcqj-6TKE6Ou5uhQ0VTnmf-NxcYKFl48aDihcGem--qa2V8GC7na549Ctj1PLXoYUbovV4LB27Kj3X83sZVnWdHqg_G0AKo4xm7hr23VUvJ1D73lEcYaGd5K9GXHNgUrJO5v288y0uCXZ5ByNDJ-K6Xi7_68dLdshlIiHaeIBuC3rhchSf2hdglkQgOyo4g4gT_HfKjwdrrpGzepNXOPQEwtUs_o2uriXAd7FfbL_Q4ORiDWPXkmwBXqo7uUfg-2SnT3DApc3PuA0";

        public BasicAuthTests(DicomFhirIntegrationTestFixture<FhirStartup> fixture)
        {
            Client = fixture.FhirFixture.FhirClient;
            Fixture = fixture;
        }

        protected FhirClient Client { get; set; }

        public DicomFhirIntegrationTestFixture<FhirStartup> Fixture { get; set; }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task WhenReadingAResource_GivenAUserWithNoReadPermissions_TheServerShouldReturnForbidden()
        {
            var resourceId = await Fixture.PostNewSampleStudyAsync();

            await Client.RunAsUser(TestUsers.WriteOnlyUser, TestApplications.NativeClient);
            FhirException fhirException = await Assert.ThrowsAsync<FhirException>(async () => await Client.ReadAsync<ImagingStudy>(ResourceType.ImagingStudy, resourceId));
            Assert.Equal(ForbiddenMessage, fhirException.Message);
            Assert.Equal(HttpStatusCode.Forbidden, fhirException.StatusCode);
        }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task WhenCreatingAResource_GivenAUserWithNoCreatePermissions_TheServerShouldReturnForbidden()
        {
            await Client.RunAsUser(TestUsers.ReadOnlyUser, TestApplications.NativeClient);
            FhirException fhirException = await Assert.ThrowsAsync<FhirException>(async () => await Client.CreateAsync(Samples.GetDefaultPatient()));
            Assert.Equal(ForbiddenMessage, fhirException.Message);
            Assert.Equal(HttpStatusCode.Forbidden, fhirException.StatusCode);
        }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task WhenCreatingAResource_GivenAClientWithNoAuthToken_TheServerShouldReturnUnauthorized()
        {
            await Client.RunAsClientApplication(TestApplications.InvalidClient);

            FhirException fhirException = await Assert.ThrowsAsync<FhirException>(async () => await Client.CreateAsync(Samples.GetDefaultObservation()));
            Assert.Equal(UnauthorizedMessage, fhirException.Message);
            Assert.Equal(HttpStatusCode.Unauthorized, fhirException.StatusCode);
        }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task WhenCreatingAResource_GivenAClientWithInvalidAuthToken_TheServerShouldReturnUnauthorized()
        {
            await Client.RunAsClientApplication(TestApplications.InvalidClient);
            Client.HttpClient.SetBearerToken(Invalidtoken);
            FhirException fhirException = await Assert.ThrowsAsync<FhirException>(async () => await Client.CreateAsync(Samples.GetDefaultObservation()));
            Assert.Equal(UnauthorizedMessage, fhirException.Message);
            Assert.Equal(HttpStatusCode.Unauthorized, fhirException.StatusCode);
        }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task WhenCreatingAResource_GivenAClientWithWrongAudience_TheServerShouldReturnUnauthorized()
        {
            await Client.RunAsClientApplication(TestApplications.WrongAudienceClient);
            FhirException fhirException = await Assert.ThrowsAsync<FhirException>(async () => await Client.CreateAsync(Samples.GetDefaultObservation()));
            Assert.Equal(UnauthorizedMessage, fhirException.Message);
            Assert.Equal(HttpStatusCode.Unauthorized, fhirException.StatusCode);
        }
    }
}
