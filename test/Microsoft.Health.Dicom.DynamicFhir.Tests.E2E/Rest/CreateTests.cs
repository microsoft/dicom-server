// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.DynamicFhir.Web;
using Microsoft.Health.Fhir.Tests.Common;
using Microsoft.Health.Fhir.Tests.Common.FixtureParameters;
using Microsoft.Health.Fhir.Tests.E2E.Common;
using Xunit;
using FhirClient = Microsoft.Health.Dicom.DynamicFhir.Tests.E2E.Common.FhirClient;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Dicom.DynamicFhir.Tests.E2E.Rest
{
    [HttpIntegrationFixtureArgumentSets(DataStore.CosmosDb, Format.Json | Format.Xml)]
    public class CreateTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        public CreateTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            Client = fixture.FhirClient;
        }

        protected FhirClient Client { get; set; }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task WhenPostingToHttp_GivenAResource_TheServerShouldRespondMethodNotAllowed()
        {
            FhirException ex = await Assert.ThrowsAsync<FhirException>(
                () => Client.CreateAsync(Samples.GetDefaultPatient()));

            Assert.Equal(System.Net.HttpStatusCode.MethodNotAllowed, ex.StatusCode);
        }
    }
}
