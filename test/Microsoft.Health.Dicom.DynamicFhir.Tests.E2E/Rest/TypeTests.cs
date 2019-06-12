// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
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
    [HttpIntegrationFixtureArgumentSets(DataStore.CosmosDb, Format.Json | Format.Xml)]
    public class TypeTests : IClassFixture<DicomFhirIntegrationTestFixture<FhirStartup>>
    {
        public TypeTests(DicomFhirIntegrationTestFixture<FhirStartup> fixture)
        {
            FhirClient = fixture.FhirFixture.FhirClient;
            Fixture = fixture;
        }

        protected FhirClient FhirClient { get; set; }

        public DicomFhirIntegrationTestFixture<FhirStartup> Fixture { get; set; }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task WhenReadingAnUnsupportedResourceType_TheServerShouldReturnMethodNotAllowed()
        {
            FhirResponse<CapabilityStatement> readResponse = await FhirClient.ReadAsync<CapabilityStatement>("metadata");
            var capabilities = readResponse.Resource;

            var readResources = capabilities.Rest.First().Resource.Where(
                r => r.Interaction.First().Code == CapabilityStatement.TypeRestfulInteraction.Read)
                    .Select(r => r.Type);

            var allResources = ModelInfo.SupportedResources.Select(r => (ResourceType)Enum.Parse(typeof(ResourceType), r));

            var unavailableResources = allResources.Where(r => !readResources.Contains(r));

            foreach (var resource in unavailableResources)
            {
                FhirException ex = await Assert.ThrowsAsync<FhirException>(
                () => FhirClient.ReadUntypedAsync(resource, "1"));

                Assert.Equal(System.Net.HttpStatusCode.MethodNotAllowed, ex.StatusCode);
            }
        }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task WhenGettingAResource_GivenANonExistantId_TheServerShouldReturnANotFoundStatus()
        {
            FhirException ex = await Assert.ThrowsAsync<FhirException>(
                () => FhirClient.ReadAsync<ImagingStudy>(ResourceType.ImagingStudy, Guid.NewGuid().ToString()));

            Assert.Equal(System.Net.HttpStatusCode.NotFound, ex.StatusCode);
        }
    }
}
