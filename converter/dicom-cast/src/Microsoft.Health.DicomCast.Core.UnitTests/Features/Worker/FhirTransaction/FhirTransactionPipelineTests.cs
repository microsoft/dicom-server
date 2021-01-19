// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Exceptions;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using NSubstitute;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class FhirTransactionPipelineTests
    {
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private static readonly FhirTransactionRequestEntry PatientRequestEntry = FhirTransactionRequestEntryGenerator.GenerateDefaultNoChangeRequestEntry<Patient>(
            new ServerResourceId(ResourceType.Parameters, "p1"));

        private static readonly FhirTransactionRequestEntry ImagingStudyRequestEntry = FhirTransactionRequestEntryGenerator.GenerateDefaultUpdateRequestEntry<ImagingStudy>(
            new ServerResourceId(ResourceType.ImagingStudy, "is1"));

        private readonly IList<IFhirTransactionPipelineStep> _fhirTransactionPipelineSteps = new List<IFhirTransactionPipelineStep>();
        private readonly FhirTransactionRequestResponsePropertyAccessors _fhirTransactionRequestResponsePropertyAccessors = new FhirTransactionRequestResponsePropertyAccessors();
        private readonly IFhirTransactionExecutor _fhirTransactionExecutor = Substitute.For<IFhirTransactionExecutor>();

        private readonly FhirTransactionPipeline _fhirTransactionPipeline;

        private readonly IFhirTransactionPipelineStep _captureFhirTransactionContextStep = Substitute.For<IFhirTransactionPipelineStep>();

        private FhirTransactionContext _capturedFhirTransactionContext;

        public FhirTransactionPipelineTests()
        {
            // Use this step to capture the context. The same context will be used across all steps.
            _captureFhirTransactionContextStep.When(pipeline => pipeline.PrepareRequestAsync(Arg.Any<FhirTransactionContext>(), DefaultCancellationToken))
                .Do(callback =>
                {
                    FhirTransactionContext context = callback.ArgAt<FhirTransactionContext>(0);

                    _capturedFhirTransactionContext = context;
                });

            _fhirTransactionPipelineSteps.Add(_captureFhirTransactionContextStep);

            _fhirTransactionPipeline = new FhirTransactionPipeline(
                _fhirTransactionPipelineSteps,
                _fhirTransactionRequestResponsePropertyAccessors,
                _fhirTransactionExecutor);
        }

        [Theory]
        [InlineData(FhirTransactionRequestMode.Create)]
        [InlineData(FhirTransactionRequestMode.Update)]
        public async Task GivenAResourceToProcess_WhenProcessed_ThenTransactionShouldBeExecuted(FhirTransactionRequestMode requestMode)
        {
            // Setup the pipeline step to simulate creating/updating patient.
            var patientRequest = new FhirTransactionRequestEntry(
                requestMode,
                new Bundle.RequestComponent(),
                new ClientResourceId(),
                new Patient());

            var pipelineStep = new MockFhirTransactionPipelineStep()
            {
                OnPrepareRequestAsyncCalled = (context, cancellationToken) =>
                {
                    context.Request.Patient = patientRequest;

                    Assert.Equal(DefaultCancellationToken, cancellationToken);
                },
            };

            _fhirTransactionPipelineSteps.Add(pipelineStep);

            // Setup the transaction executor to return response.
            var responseBundle = new Bundle();

            var responseEntry = new Bundle.EntryComponent()
            {
                Response = new Bundle.ResponseComponent(),
                Resource = new Patient(),
            };

            responseBundle.Entry.Add(responseEntry);

            _fhirTransactionExecutor.ExecuteTransactionAsync(
                Arg.Any<Bundle>(),
                DefaultCancellationToken)
                .Returns(call =>
                {
                    // Make sure the request bundle is correct.
                    Bundle requestBundle = call.ArgAt<Bundle>(0);

                    Assert.NotNull(requestBundle);
                    Assert.Equal(Bundle.BundleType.Transaction, requestBundle.Type);

                    Assert.Collection(
                        requestBundle.Entry,
                        entry =>
                        {
                            Assert.Equal(patientRequest.ResourceId.ToString(), entry.FullUrl);
                            Assert.Equal(patientRequest.Request, entry.Request);
                            Assert.Equal(patientRequest.Resource, entry.Resource);
                        });

                    return responseBundle;
                });

            // Process
            await _fhirTransactionPipeline.ProcessAsync(ChangeFeedGenerator.Generate(), DefaultCancellationToken);

            // The response should have been processed.
            Assert.NotNull(_capturedFhirTransactionContext);

            FhirTransactionResponseEntry patientResponse = _capturedFhirTransactionContext.Response.Patient;

            Assert.NotNull(patientResponse);
            Assert.Equal(responseEntry.Response, patientResponse.Response);
            Assert.Equal(responseEntry.Resource, patientResponse.Resource);
        }

        [Fact]
        public async Task WhenThrowAnExceptionInProcess_ThrowTheSameException()
        {
            var pipelineStep = new MockFhirTransactionPipelineStep()
            {
                OnPrepareRequestAsyncCalled = (context, cancellationToken) =>
                {
                    throw new Exception();
                },
            };

            _fhirTransactionPipelineSteps.Add(pipelineStep);

            // Process
            await Assert.ThrowsAsync<Exception>(() => _fhirTransactionPipeline.ProcessAsync(ChangeFeedGenerator.Generate(), DefaultCancellationToken));
        }

        [Fact]
        public async Task WhenThrowAnResourceConflictExceptionInProcess_ThrowRetryableExceptionException()
        {
            var pipelineStep = new MockFhirTransactionPipelineStep()
            {
                OnPrepareRequestAsyncCalled = (context, cancellationToken) =>
                {
                    throw new ResourceConflictException();
                },
            };

            _fhirTransactionPipelineSteps.Add(pipelineStep);

            // Process
            await Assert.ThrowsAsync<RetryableException>(() => _fhirTransactionPipeline.ProcessAsync(ChangeFeedGenerator.Generate(), DefaultCancellationToken));
        }

        [Fact]
        public async Task WhenThrowAServerTooBusyExceptionInProcess_ThrowRetryableExceptionException()
        {
            var pipelineStep = new MockFhirTransactionPipelineStep()
            {
                OnPrepareRequestAsyncCalled = (context, cancellationToken) =>
                {
                    throw new ServerTooBusyException();
                },
            };

            _fhirTransactionPipelineSteps.Add(pipelineStep);

            // Process
            await Assert.ThrowsAsync<RetryableException>(() => _fhirTransactionPipeline.ProcessAsync(ChangeFeedGenerator.Generate(), DefaultCancellationToken));
        }

        [Fact]
        public async Task WhenThrowATaskCanceledExceptionInProcess_ThrowRetryableExceptionException()
        {
            var pipelineStep = new MockFhirTransactionPipelineStep()
            {
                OnPrepareRequestAsyncCalled = (context, cancellationToken) =>
                {
                    throw new TaskCanceledException();
                },
            };

            _fhirTransactionPipelineSteps.Add(pipelineStep);

            // Process
            await Assert.ThrowsAsync<RetryableException>(() => _fhirTransactionPipeline.ProcessAsync(ChangeFeedGenerator.Generate(), DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenNoResourceToProcess_WhenProcessed_ThenTransactionShouldBeExecuted()
        {
            // Setup the pipeline step to simulate no requests.
            IFhirTransactionPipelineStep pipelineStep = Substitute.For<IFhirTransactionPipelineStep>();

            _fhirTransactionPipelineSteps.Add(pipelineStep);

            // Process
            await _fhirTransactionPipeline.ProcessAsync(ChangeFeedGenerator.Generate(), DefaultCancellationToken);

            // There should not be any response.
            pipelineStep.DidNotReceiveWithAnyArgs().ProcessResponse(default);
        }

        [Fact]
        public async Task GivenResourcesInMixedState_WhenProcessed_ThenOnlyResourceWithChangesShouldBeProcessed()
        {
            // Setup the pipeline step to simulate updating an existing patient.
            FhirTransactionRequestEntry patientRequest = FhirTransactionRequestEntryGenerator.GenerateDefaultUpdateRequestEntry<Patient>(
                new ServerResourceId(ResourceType.Patient, "p1"));

            var patientStep = new MockFhirTransactionPipelineStep()
            {
                OnPrepareRequestAsyncCalled = (context, cancellationToken) =>
                {
                    context.Request.Patient = patientRequest;
                },
            };

            // Setup the pipeline step to simulate no update to endpoint.
            FhirTransactionRequestEntry endpointRequest = FhirTransactionRequestEntryGenerator.GenerateDefaultNoChangeRequestEntry<Endpoint>(
                new ServerResourceId(ResourceType.Endpoint, "123"));

            var endpointStep = new MockFhirTransactionPipelineStep()
            {
                OnPrepareRequestAsyncCalled = (context, cancellationToken) =>
                {
                    context.Request.Endpoint = endpointRequest;
                },
            };

            // Setup the pipeline step to simulate creating a new imaging study.
            var imagingStudyRequest = FhirTransactionRequestEntryGenerator.GenerateDefaultCreateRequestEntry<ImagingStudy>();

            var imagingStudyStep = new MockFhirTransactionPipelineStep()
            {
                OnPrepareRequestAsyncCalled = (context, cancellationToken) =>
                {
                    context.Request.ImagingStudy = imagingStudyRequest;
                },
            };

            _fhirTransactionPipelineSteps.Add(patientStep);
            _fhirTransactionPipelineSteps.Add(endpointStep);
            _fhirTransactionPipelineSteps.Add(imagingStudyStep);

            // Setup the transaction executor to return response.
            // The properties will be processed in alphabetical order.
            var responseBundle = new Bundle();

            var imagingStudyResponseEntry = new Bundle.EntryComponent()
            {
                Response = new Bundle.ResponseComponent(),
                Resource = new ImagingStudy(),
            };

            var patientResponseEntry = new Bundle.EntryComponent()
            {
                Response = new Bundle.ResponseComponent(),
                Resource = new Patient(),
            };

            responseBundle.Entry.Add(imagingStudyResponseEntry);
            responseBundle.Entry.Add(patientResponseEntry);

            _fhirTransactionExecutor.ExecuteTransactionAsync(
                Arg.Any<Bundle>(),
                DefaultCancellationToken)
                .Returns(call =>
                {
                    // Make sure the request bundle is correct.
                    Bundle requestBundle = call.ArgAt<Bundle>(0);

                    var expectedEntries = new Bundle.EntryComponent[]
                    {
                        new Bundle.EntryComponent()
                        {
                            FullUrl = imagingStudyRequest.ResourceId.ToString(),
                            Request = imagingStudyRequest.Request,
                            Resource = imagingStudyRequest.Resource,
                        },
                        new Bundle.EntryComponent()
                        {
                            FullUrl = "Patient/p1",
                            Request = patientRequest.Request,
                            Resource = patientRequest.Resource,
                        },
                    };

                    Assert.True(
                        requestBundle.Entry.Matches(expectedEntries));

                    return responseBundle;
                });

            // Process
            await _fhirTransactionPipeline.ProcessAsync(ChangeFeedGenerator.Generate(), DefaultCancellationToken);

            // The response should have been processed.
            Assert.NotNull(_capturedFhirTransactionContext);

            FhirTransactionResponseEntry endpointResponse = _capturedFhirTransactionContext.Response.Endpoint;

            FhirTransactionResponseEntry imaingStudyResponse = _capturedFhirTransactionContext.Response.ImagingStudy;

            Assert.NotNull(imaingStudyResponse);
            Assert.Same(imaingStudyResponse.Response, imagingStudyResponseEntry.Response);
            Assert.Same(imaingStudyResponse.Resource, imagingStudyResponseEntry.Resource);

            Assert.Null(endpointResponse);

            FhirTransactionResponseEntry patientResponse = _capturedFhirTransactionContext.Response.Patient;

            Assert.NotNull(patientResponse);
            Assert.Same(patientResponse.Response, patientResponseEntry.Response);
            Assert.Same(patientResponse.Resource, patientResponseEntry.Resource);
        }
    }
}
