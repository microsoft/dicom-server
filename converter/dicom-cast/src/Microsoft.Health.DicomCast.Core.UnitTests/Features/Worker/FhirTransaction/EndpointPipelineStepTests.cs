// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Extensions;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using NSubstitute;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class EndpointPipelineStepTests
    {
        private const string EndpointConnectionTypeSystem = "http://terminology.hl7.org/CodeSystem/endpoint-connection-type";
        private const string EndpointConnectionTypeCode = "dicom-wado-rs";
        private const string EndpointName = "DICOM WADO-RS endpoint";
        private const string EndpointPayloadTypeText = "DICOM WADO-RS";
        private const string DicomMimeType = "application/dicom";

        private const string DefaultDicomWebEndpoint = "https://dicom/";

        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private readonly DicomWebConfiguration _configuration;
        private readonly EndpointPipelineStep _endpointPipeline;
        private readonly IFhirService _fhirService;

        public EndpointPipelineStepTests()
        {
            _configuration = new DicomWebConfiguration() { Endpoint = new System.Uri(DefaultDicomWebEndpoint), };

            IOptions<DicomWebConfiguration> optionsConfiguration = Options.Create(_configuration);

            _fhirService = Substitute.For<IFhirService>();

            _endpointPipeline = new EndpointPipelineStep(optionsConfiguration, _fhirService);
        }

        [Fact]
        public async Task GivenEndpointDoesNotAlreadyExist_WhenRequestIsPrepared_ThenCorrentRequestEntryShouldBeCreated()
        {
            var context = new FhirTransactionContext(ChangeFeedGenerator.Generate());

            await _endpointPipeline.PrepareRequestAsync(context, DefaultCancellationToken);

            FhirTransactionRequestEntry actualEndpointEntry = context.Request.Endpoint;

            ValidationUtility.ValidateRequestEntryMinimumRequirementForWithChange(FhirTransactionRequestMode.Create, "Endpoint", Bundle.HTTPVerb.POST, actualEndpointEntry);

            Assert.Equal($"name={EndpointName}&connection-type={EndpointConnectionTypeSystem}|{EndpointConnectionTypeCode}", actualEndpointEntry.Request.IfNoneExist);

            Endpoint endpoint = Assert.IsType<Endpoint>(actualEndpointEntry.Resource);

            Assert.Equal(EndpointName, endpoint.Name);
            Assert.Equal(Endpoint.EndpointStatus.Active, endpoint.Status);
            Assert.NotNull(endpoint.ConnectionType);
            Assert.Equal(EndpointConnectionTypeSystem, endpoint.ConnectionType.System);
            Assert.Equal(EndpointConnectionTypeCode, endpoint.ConnectionType.Code);
            Assert.Equal(_configuration.Endpoint.ToString(), endpoint.Address);
            Assert.Equal(EndpointPayloadTypeText, endpoint.PayloadType.First().Text);
            Assert.Equal(new[] { DicomMimeType }, endpoint.PayloadMimeType);
        }

        [Fact]
        public async Task GivenAnExistingEndpointWithMatchingAddress_WhenRequestIsPrepared_ThenCorrectRequestEntryShouldBeCreated()
        {
            var context = new FhirTransactionContext(ChangeFeedGenerator.Generate());

            Endpoint endpoint = FhirResourceBuilder.CreateEndpointResource(address: DefaultDicomWebEndpoint);

            _fhirService.RetrieveEndpointAsync(Arg.Any<string>(), DefaultCancellationToken).Returns(endpoint);

            await _endpointPipeline.PrepareRequestAsync(context, DefaultCancellationToken);

            FhirTransactionRequestEntry actualEndPointEntry = context.Request.Endpoint;

            ValidationUtility.ValidateRequestEntryMinimumRequirementForNoChange(endpoint.ToServerResourceId(), actualEndPointEntry);
        }

        [Fact]
        public async Task GivenAnExistingEndpointWithDifferentAddress_WhenRequestIsPrepared_ThenFhirResourceValidationExceptionShouldBeThrown()
        {
            var context = new FhirTransactionContext(ChangeFeedGenerator.Generate());

            Endpoint endpoint = FhirResourceBuilder.CreateEndpointResource(address: "https://dicom2");

            _fhirService.RetrieveEndpointAsync(Arg.Any<string>(), DefaultCancellationToken).Returns(endpoint);

            await Assert.ThrowsAsync<FhirResourceValidationException>(() => _endpointPipeline.PrepareRequestAsync(context, DefaultCancellationToken));
        }
    }
}
