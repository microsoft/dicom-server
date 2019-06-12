// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
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
    public class ReadTests : IClassFixture<DicomFhirIntegrationTestFixture<FhirStartup>>
    {
        public ReadTests(DicomFhirIntegrationTestFixture<FhirStartup> fixture)
        {
            FhirClient = fixture.FhirFixture.FhirClient;
            Fixture = fixture;
        }

        protected FhirClient FhirClient { get; set; }

        public DicomFhirIntegrationTestFixture<FhirStartup> Fixture { get; set; }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task WhenGettingAResource_GivenAnId_TheServerShouldReturnTheAppropriateResourceSuccessfully()
        {
            var studyInstanceUid = await Fixture.PostNewSampleStudyAsync();

            FhirResponse<ImagingStudy> readResponse = await FhirClient.ReadAsync<ImagingStudy>(ResourceType.ImagingStudy, studyInstanceUid);

            ImagingStudy readResource = readResponse.Resource;

            Assert.Equal(studyInstanceUid, readResource.Id);
            TestHelper.AssertSecurityHeaders(readResponse.Headers);
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
