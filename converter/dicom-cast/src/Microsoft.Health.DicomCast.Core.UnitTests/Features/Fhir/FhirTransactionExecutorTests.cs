// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using Microsoft.Health.DicomCast.Core.Exceptions;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Microsoft.Health.Fhir.Client;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Fhir
{
    public class FhirTransactionExecutorTests
    {
        private readonly IFhirClient _fhirClient = Substitute.For<IFhirClient>();
        private readonly FhirTransactionExecutor _fhirTransactionExecutor;

        private readonly Bundle _defaultRequestBundle = new Bundle();

        public FhirTransactionExecutorTests()
        {
            _fhirTransactionExecutor = new FhirTransactionExecutor(_fhirClient);
        }

        [Fact]
        public async Task GivenRequestFailsWithPreconditionFailed_WhenExecuting_ThenResourceConflictExceptionShouldBeThrown()
        {
            SetupPostException(HttpStatusCode.PreconditionFailed);

            await Assert.ThrowsAsync<ResourceConflictException>(() => _fhirTransactionExecutor.ExecuteTransactionAsync(new Bundle(), default));
        }

        [Fact]
        public async Task GivenRequestFailsWithTooManyRequests_WhenExecuting_ThenServerTooBusyExceptionShouldBeThrown()
        {
            SetupPostException(HttpStatusCode.TooManyRequests);

            await Assert.ThrowsAsync<ServerTooBusyException>(() => _fhirTransactionExecutor.ExecuteTransactionAsync(new Bundle(), default));
        }

        [Fact]
        public async Task GivenRequestFailsWithAnyOtherReason_WhenExecuting_ThenTransactionFailedExceptionShouldBeThrown()
        {
            var expectedOperationOutcome = new OperationOutcome();
            SetupPostException(HttpStatusCode.NotFound, expectedOperationOutcome);

            TransactionFailedException exception = await Assert.ThrowsAsync<TransactionFailedException>(
                () => _fhirTransactionExecutor.ExecuteTransactionAsync(new Bundle(), default));

            Assert.Same(expectedOperationOutcome, exception.OperationOutcome);
        }

        [Fact]
        public async Task GivenNullResponse_WhenTransactionIsExecuted_ThenInvalidFhirResponseExceptionShouldBeThrown()
        {
            var bundle = new Bundle();

            _fhirClient.PostBundleAsync(bundle).Returns(new FhirResponse<Bundle>(new HttpResponseMessage(), null));

            await Assert.ThrowsAsync<InvalidFhirResponseException>(() => _fhirTransactionExecutor.ExecuteTransactionAsync(bundle, default));
        }

        [Fact]
        public async Task GivenResponseBundleEntryCountDoesNotMatch_WhenTransactionIsExecuted_ThenInvalidFhirResponseExceptionShouldBeThrown()
        {
            // Request will have 1 entry but response will have 0.
            AddEntryComponentToDefaultRequestBundle();

            _fhirClient.PostBundleAsync(_defaultRequestBundle).Returns(new FhirResponse<Bundle>(new HttpResponseMessage(HttpStatusCode.OK), new Bundle()));

            await Assert.ThrowsAsync<InvalidFhirResponseException>(() => ExecuteTransactionAsync());
        }

        [Fact]
        public async Task GivenRequestsIsSuccessful_WhenTransactionIsExecuted_ThenCorrectBundleShouldBeReturned()
        {
            Bundle expectedBundle = SetupTransaction("200");

            Bundle actualBundle = await ExecuteTransactionAsync();

            Assert.Same(expectedBundle, actualBundle);

            // The annotation should be set.
            Assert.True(actualBundle.Entry[0].Response.TryGetAnnotation(typeof(HttpStatusCode), out object statusCode));

            Assert.Equal(HttpStatusCode.OK, (HttpStatusCode)statusCode);
        }

        [Fact]
        public async Task GivenNullEntryComponent_WhenTransactionIsExecuted_ThenInvalidFhirResponseExceptionShouldBeThrown()
        {
            var bundle = new Bundle();

            bundle.Entry.Add(null);
            _fhirClient.PostBundleAsync(_defaultRequestBundle).Returns(new FhirResponse<Bundle>(new HttpResponseMessage(HttpStatusCode.OK), bundle));

            await Assert.ThrowsAsync<InvalidFhirResponseException>(() => ExecuteTransactionAsync());
        }

        [Fact]
        public async Task GivenNullEntryComponentResponse_WhenTransactionIsExecuted_ThenInvalidFhirResponseExceptionShouldBeThrown()
        {
            var bundle = new Bundle();

            bundle.Entry.Add(new Bundle.EntryComponent());
            _fhirClient.PostBundleAsync(_defaultRequestBundle).Returns(new FhirResponse<Bundle>(new HttpResponseMessage(HttpStatusCode.OK), bundle));

            await Assert.ThrowsAsync<InvalidFhirResponseException>(() => ExecuteTransactionAsync());
        }

        [Fact]
        public async Task GivenNullEntryComponentResponseStatus_WhenTransactionIsExecuted_ThenInvalidFhirResponseExceptionShouldBeThrown()
        {
            var bundle = new Bundle();

            bundle.Entry.Add(new Bundle.EntryComponent()
            {
                Response = new Bundle.ResponseComponent(),
            });
            _fhirClient.PostBundleAsync(_defaultRequestBundle).Returns(new FhirResponse<Bundle>(new HttpResponseMessage(HttpStatusCode.OK), bundle));

            await Assert.ThrowsAsync<InvalidFhirResponseException>(() => ExecuteTransactionAsync());
        }

        [Theory]
        [InlineData("")]
        [InlineData("10")]
        [InlineData("1000")]
        [InlineData("10a")]
        [InlineData("299")]
        public async Task GivenInvalidEntryComponentResponseStatusValue_WhenTransactionIsExecuted_ThenInvalidFhirResponseExceptionShouldBeThrown(string invalidStatus)
        {
            var bundle = new Bundle();

            bundle.Entry.Add(new Bundle.EntryComponent()
            {
                Response = new Bundle.ResponseComponent()
                {
                    Status = invalidStatus,
                },
            });
            _fhirClient.PostBundleAsync(_defaultRequestBundle).Returns(new FhirResponse<Bundle>(new HttpResponseMessage(HttpStatusCode.OK), bundle));

            await Assert.ThrowsAsync<InvalidFhirResponseException>(() => ExecuteTransactionAsync());
        }

        private Bundle SetupTransaction(params string[] statusList)
        {
            for (int i = 0; i < statusList.Length; i++)
            {
                AddEntryComponentToDefaultRequestBundle();
            }

            Bundle bundle = GenerateBundleWithStatus(statusList);

            _fhirClient.PostBundleAsync(Arg.Any<Bundle>()).Returns(new FhirResponse<Bundle>(new HttpResponseMessage(HttpStatusCode.OK), bundle));

            return bundle;
        }

        private void AddEntryComponentToDefaultRequestBundle()
        {
            _defaultRequestBundle.Entry.Add(new Bundle.EntryComponent());
        }

        private Task<Bundle> ExecuteTransactionAsync()
            => _fhirTransactionExecutor.ExecuteTransactionAsync(_defaultRequestBundle, default);

        private static Bundle GenerateBundleWithStatus(params string[] statusList)
        {
            var bundle = new Bundle();

            foreach (string status in statusList)
            {
                bundle.Entry.Add(new Bundle.EntryComponent()
                {
                    Response = new Bundle.ResponseComponent()
                    {
                        Status = status,
                    },
                });
            }

            return bundle;
        }

        private void SetupPostException(HttpStatusCode httpStatusCode, OperationOutcome operationOutcome = null)
        {
            if (operationOutcome == null)
            {
                operationOutcome = new OperationOutcome();
            }

            var response = new FhirResponse<OperationOutcome>(new HttpResponseMessage(httpStatusCode), operationOutcome);
            _fhirClient.PostBundleAsync(default).ThrowsForAnyArgs(new FhirException(response));
        }
    }
}
