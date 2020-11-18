// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Extensions;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Microsoft.Health.Fhir.Client;
using NSubstitute;
using Xunit;
using static Hl7.Fhir.Model.CapabilityStatement;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Fhir
{
    public class FhirServiceTests
    {
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;
        private static readonly Identifier DefaultPatientIdentifier = new Identifier(string.Empty, "p1");
        private static readonly Identifier DefaultImagingStudyIdentifier = new Identifier(string.Empty, "123");
        private const string MetaDataEndpoint = "metadata";

        private readonly IFhirClient _fhirClient = Substitute.For<IFhirClient>();
        private readonly IFhirResourceValidator _fhirResourceValidator = Substitute.For<IFhirResourceValidator>();
        private readonly FhirService _fhirService;

        public FhirServiceTests()
        {
            var fhirConfigurationOptions = Substitute.For<IOptions<FhirConfiguration>>();
            fhirConfigurationOptions.Value.Returns(new FhirConfiguration { Authentication = new AuthenticationConfiguration() });
            _fhirService = new FhirService(_fhirClient, _fhirResourceValidator, fhirConfigurationOptions);
        }

        private delegate Task<TResource> RetrieveAsyncDelegate<TResource>(Identifier identifier, CancellationToken cancellationToken);

        [Fact]
        public async Task GivenNoMatchingResource_WhenPatientIsRetrieved_ThenItShouldNotBeValidated()
        {
            await ExecuteAndValidateNoMatch(DefaultPatientIdentifier, _fhirService.RetrievePatientAsync);
        }

        [Fact]
        public async Task GivenASingleMatch_WhenPatientIsRetrieved_ThenItShouldBeValidated()
        {
            await ExecuteAndValidateSingleMatchAsync<Patient>(DefaultPatientIdentifier, _fhirService.RetrievePatientAsync);
        }

        [Fact]
        public async Task GivenASingleMatchNotInFirstResultSet_WhenPatientIsRetrieved_ThenCorrectPatientShouldBeReturned()
        {
            await ExecuteAndValidateSingleMatchNotInFirstResultSetAsync<Patient>(DefaultPatientIdentifier, _fhirService.RetrievePatientAsync);
        }

        [Fact]
        public async Task GivenMultipleMatches_WhenPatientIsRetrieved_ThenMultipleMatchingResourcesExceptionShouldBeThrown()
        {
            await ExecuteAndValidateMultipleMatchesAsync<Patient>(_fhirService.RetrievePatientAsync);
        }

        [Fact]
        public async Task GivenMultipleMatchesThatSpansMultipleResultSet_WhenPatientIsRetrieved_ThenMultipleMatchingResourcesExceptionShouldBeThrown()
        {
            await ExecuteAndValidateMultipleMatchesThatSpansInMultipleResultSetAsync(DefaultImagingStudyIdentifier, _fhirService.RetrievePatientAsync);
        }

        [Fact]
        public async Task GivenNoMatchingResource_WhenImagingStudyIsRetrieved_ThenItShouldNotBeValidated()
        {
            await ExecuteAndValidateNoMatch(DefaultPatientIdentifier, _fhirService.RetrieveImagingStudyAsync);
        }

        [Fact]
        public async Task GivenASingleMatch_WhenImagingStudyIsRetrieved_ThenItShouldBeValidated()
        {
            await ExecuteAndValidateSingleMatchAsync<ImagingStudy>(DefaultImagingStudyIdentifier, _fhirService.RetrieveImagingStudyAsync);
        }

        [Fact]
        public async Task GivenASingleMatchNotInFirstResultSet_WhenImagingStudyIsRetrieved_ThenCorrectImagingStudyShouldBeReturned()
        {
            await ExecuteAndValidateSingleMatchNotInFirstResultSetAsync<ImagingStudy>(DefaultImagingStudyIdentifier, _fhirService.RetrieveImagingStudyAsync);
        }

        [Fact]
        public async Task GivenMultipleMatches_WhenImagingStudyIsRetrieved_ThenMultipleMatchingResourcesExceptionShouldBeThrown()
        {
            await ExecuteAndValidateMultipleMatchesAsync<ImagingStudy>(_fhirService.RetrieveImagingStudyAsync);
        }

        [Fact]
        public async Task GivenMultipleMatchesThatSpansMultipleResultSet_WhenImagingStudyIsRetrieved_ThenMultipleMatchingResourcesExceptionShouldBeThrown()
        {
            await ExecuteAndValidateMultipleMatchesThatSpansInMultipleResultSetAsync(DefaultImagingStudyIdentifier, _fhirService.RetrieveImagingStudyAsync);
        }

        [Fact]
        public async Task GivenInValidFhirConfigVersion_ShouldThrowError()
        {
            _fhirClient.ReadAsync<CapabilityStatement>(MetaDataEndpoint, DefaultCancellationToken).Returns(GenerateFhirCapabilityResponse(FHIRVersion.N0_01, SystemRestfulInteraction.Transaction));
            await Assert.ThrowsAsync<InvalidFhirServerException>(() => _fhirService.ValidateFhirService(DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenInValidFhirConfigInteraction_ShouldThrowError()
        {
            _fhirClient.ReadAsync<CapabilityStatement>(MetaDataEndpoint, DefaultCancellationToken).Returns(GenerateFhirCapabilityResponse(FHIRVersion.N4_0_0, SystemRestfulInteraction.Batch));
            await Assert.ThrowsAsync<InvalidFhirServerException>(() => _fhirService.ValidateFhirService(DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenValidFhirConfigV4_ShouldNotThrowError()
        {
            _fhirClient.ReadAsync<CapabilityStatement>(MetaDataEndpoint, DefaultCancellationToken).Returns(GenerateFhirCapabilityResponse(FHIRVersion.N4_0_0, SystemRestfulInteraction.Transaction));
            await _fhirService.ValidateFhirService(DefaultCancellationToken);
        }

        [Fact]
        public async Task GivenValidFhirConfigV401_ShouldNotThrowError()
        {
            _fhirClient.ReadAsync<CapabilityStatement>(MetaDataEndpoint, DefaultCancellationToken).Returns(GenerateFhirCapabilityResponse(FHIRVersion.N4_0_1, SystemRestfulInteraction.Transaction));
            await _fhirService.ValidateFhirService(DefaultCancellationToken);
        }

        private void SetupIdentifierSearchCriteria(ResourceType resourceType, Identifier identifier, Bundle bundle)
        {
            _fhirClient.SearchAsync(
                resourceType,
                identifier.ToSearchQueryParameter(),
                count: null,
                DefaultCancellationToken)
                .Returns(GenerateFhirResponse(bundle));
        }

        private async Task ExecuteAndValidateNoMatch<TResource>(Identifier identifier, RetrieveAsyncDelegate<TResource> retrieve)
            where TResource : Resource, new()
        {
            SetupIdentifierSearchCriteria(new TResource().ResourceType, identifier, new Bundle());

            TResource resource = await retrieve(identifier, DefaultCancellationToken);

            Assert.Null(resource);
            _fhirResourceValidator.DidNotReceiveWithAnyArgs().Validate(default);
        }

        private async Task ExecuteAndValidateSingleMatchAsync<TResource>(Identifier identifier, RetrieveAsyncDelegate<TResource> retrieve)
            where TResource : Resource, new()
        {
            var expectedResource = new TResource();

            var bundle = new Bundle();

            bundle.Entry.Add(
                new Bundle.EntryComponent()
                {
                    Resource = expectedResource,
                });

            SetupIdentifierSearchCriteria(expectedResource.ResourceType, identifier, bundle);

            TResource actualResource = await retrieve(identifier, DefaultCancellationToken);

            Assert.Same(expectedResource, actualResource);
            _fhirResourceValidator.Received(1).Validate(expectedResource);
        }

        private async Task ExecuteAndValidateSingleMatchNotInFirstResultSetAsync<TResource>(Identifier identifier, RetrieveAsyncDelegate<TResource> retrieve)
            where TResource : Resource, new()
        {
            var expectedResource = new TResource();

            var firstBundle = new Bundle()
            {
                NextLink = new Uri("next", UriKind.Relative),
            };

            _fhirClient.SearchAsync(
                expectedResource.ResourceType,
                query: Arg.Any<string>(),
                count: Arg.Any<int?>(),
                cancellationToken: DefaultCancellationToken)
                .Returns(GenerateFhirResponse(firstBundle));

            var bundle = new Bundle();

            bundle.Entry.Add(
                new Bundle.EntryComponent()
                {
                    Resource = expectedResource,
                });

            _fhirClient.SearchAsync(url: Arg.Any<string>(), DefaultCancellationToken).Returns(GenerateFhirResponse(bundle));

            TResource actualResource = await retrieve(identifier, DefaultCancellationToken);

            Assert.Same(expectedResource, actualResource);
        }

        private async Task ExecuteAndValidateMultipleMatchesAsync<TResource>(RetrieveAsyncDelegate<TResource> retrieve)
            where TResource : Resource, new()
        {
            var expectedResource = new TResource();

            var bundleEntry = new Bundle.EntryComponent()
            {
                Resource = expectedResource,
            };

            var bundle = new Bundle();

            bundle.Entry.AddRange(new[] { bundleEntry, bundleEntry });

            _fhirClient.SearchAsync(
                expectedResource.ResourceType,
                query: Arg.Any<string>(),
                count: Arg.Any<int?>(),
                cancellationToken: DefaultCancellationToken)
                .Returns(GenerateFhirResponse(bundle));

            await Assert.ThrowsAsync<MultipleMatchingResourcesException>(() => retrieve(new Identifier(), DefaultCancellationToken));
        }

        private async Task ExecuteAndValidateMultipleMatchesThatSpansInMultipleResultSetAsync<TResource>(Identifier identifier, RetrieveAsyncDelegate<TResource> retrieve)
            where TResource : Resource, new()
        {
            var expectedResource = new TResource();

            var firstBundle = new Bundle()
            {
                NextLink = new Uri("next", UriKind.Relative),
            };

            var bundleEntry = new Bundle.EntryComponent()
            {
                Resource = expectedResource,
            };

            firstBundle.Entry.Add(bundleEntry);

            SetupIdentifierSearchCriteria(expectedResource.ResourceType, identifier, firstBundle);

            var secondBundle = new Bundle();

            secondBundle.Entry.Add(bundleEntry);

            _fhirClient.SearchAsync(url: Arg.Any<string>(), DefaultCancellationToken).Returns(GenerateFhirResponse(secondBundle));

            await Assert.ThrowsAsync<MultipleMatchingResourcesException>(() => retrieve(identifier, DefaultCancellationToken));
        }

        private static FhirResponse<Bundle> GenerateFhirResponse(Bundle firstBundle)
        {
            return new FhirResponse<Bundle>(new HttpResponseMessage(), firstBundle);
        }

        private static FhirResponse<CapabilityStatement> GenerateFhirCapabilityResponse(FHIRVersion version, SystemRestfulInteraction interaction)
        {
            CapabilityStatement statement = new CapabilityStatement();
            statement.FhirVersion = version;
            RestComponent restComponent = new RestComponent();
            SystemInteractionComponent interactionComponent = new SystemInteractionComponent();
            interactionComponent.Code = interaction;
            restComponent.Interaction = new List<SystemInteractionComponent> { interactionComponent };
            statement.Rest.Add(restComponent);

            return new FhirResponse<CapabilityStatement>(new HttpResponseMessage(), statement);
        }
    }
}
