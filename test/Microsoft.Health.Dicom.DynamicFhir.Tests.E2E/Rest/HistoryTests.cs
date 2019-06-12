// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using Microsoft.Health.Fhir.Tests.Common;
using Microsoft.Health.Fhir.Tests.Common.FixtureParameters;
using Microsoft.Health.Fhir.Tests.E2E.Common;
using Xunit;
using FhirClient = Microsoft.Health.Dicom.DynamicFhir.Tests.E2E.Common.FhirClient;
using FhirStartup = Microsoft.Health.Dicom.DynamicFhir.Web.Startup;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Dicom.DynamicFhir.Tests.E2E.Rest
{
    [HttpIntegrationFixtureArgumentSets(DataStore.CosmosDb, Format.Json | Format.Xml)]
    public class HistoryTests : IClassFixture<DicomFhirIntegrationTestFixture<FhirStartup>>
    {
        public HistoryTests(DicomFhirIntegrationTestFixture<FhirStartup> fixture)
        {
            Client = fixture.FhirFixture.FhirClient;
            Fixture = fixture;
        }

        protected FhirClient Client { get; set; }

        public DicomFhirIntegrationTestFixture<FhirStartup> Fixture { get; set; }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task WhenGettingResourceHistory_GivenAType_TheServerShouldReturnMethodNotAllowed()
        {
            FhirException ex = await Assert.ThrowsAsync<FhirException>(
                () => Client.SearchAsync("Patient/_history"));
            Assert.Equal(HttpStatusCode.MethodNotAllowed, ex.StatusCode);
        }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task WhenGettingResourceHistory_GivenATypeAndId_TheServerShouldReturnMethodNotAllowed()
        {
            var studyInstanceUid = await Fixture.PostNewSampleStudyAsync();

            FhirException ex = await Assert.ThrowsAsync<FhirException>(
                () => Client.SearchAsync($"Patient/{studyInstanceUid}/_history"));

            Assert.Equal(HttpStatusCode.MethodNotAllowed, ex.StatusCode);
        }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task WhenGettingSystemHistory_GivenNoType_TheServerShouldReturnMethodNotAllowed()
        {
            FhirException ex = await Assert.ThrowsAsync<FhirException>(
                () => Client.SearchAsync("_history"));

            Assert.Equal(HttpStatusCode.MethodNotAllowed, ex.StatusCode);
        }
    }
}
