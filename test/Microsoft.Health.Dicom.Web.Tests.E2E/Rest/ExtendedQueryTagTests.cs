// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class ExtendedQueryTagTests : IClassFixture<HttpIntegrationTestFixture<Startup>>, IAsyncLifetime
    {
        private readonly IDicomWebClient _client;
        private readonly DicomTagsManager _tagManager;
        private readonly DicomInstancesManager _instanceManager;
        private readonly bool _isUsingInProcTestServer;

        public ExtendedQueryTagTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            EnsureArg.IsNotNull(fixture.Client, nameof(fixture.Client));
            _client = fixture.Client;
            _isUsingInProcTestServer = fixture.IsUsingInProcTestServer;
            _tagManager = new DicomTagsManager(_client);
            _instanceManager = new DicomInstancesManager(_client);
        }

        [Fact]
        public async Task GivenExtendedQueryTag_WhenReindexing_ThenShouldSucceed()
        {
            if (_isUsingInProcTestServer)
            {
                // AzureFunction doesn't have InProc test sever, skip this test.
                return;
            }

            DicomTag weightTag = DicomTag.PatientWeight;
            DicomTag sizeTag = DicomTag.PatientSize;

            // Define DICOM files
            DicomDataset instance1 = Samples.CreateRandomInstanceDataset();
            instance1.Add(weightTag, 68.0M);
            instance1.Add(sizeTag, 1.78M);

            DicomDataset instance2 = Samples.CreateRandomInstanceDataset();
            instance2.Add(weightTag, 50.0M);
            instance2.Add(sizeTag, 1.5M);

            // Upload files
            await _instanceManager.StoreAsync(new DicomFile(instance1));
            await _instanceManager.StoreAsync(new DicomFile(instance2));

            // Add extended query tag
            OperationStatus operation = await _tagManager.AddTagsAsync(
                new AddExtendedQueryTagEntry[]
                {
                    new AddExtendedQueryTagEntry { Path = weightTag.GetPath(), VR = weightTag.GetDefaultVR().Code, Level = QueryTagLevel.Study },
                    new AddExtendedQueryTagEntry { Path = sizeTag.GetPath(), VR = sizeTag.GetDefaultVR().Code, Level = QueryTagLevel.Study },
                });
            Assert.Equal(OperationRuntimeStatus.Completed, operation.Status);

            // Check specific tag
            DicomWebResponse<GetExtendedQueryTagEntry> getResponse;

            getResponse = await _client.GetExtendedQueryTagAsync(weightTag.GetPath());
            Assert.Null((await getResponse.GetValueAsync()).Errors);

            getResponse = await _client.GetExtendedQueryTagAsync(sizeTag.GetPath());
            Assert.Null((await getResponse.GetValueAsync()).Errors);

            // Query multiple tags
            // Note: We don't necessarily need to check the tags are the above ones, as another test may have added ones beforehand
            var multipleTags = await _tagManager.GetTagsAsync(2, 0);
            Assert.Equal(2, multipleTags.Count);

            Assert.Equal(multipleTags[0].Path, (await _tagManager.GetTagsAsync(1, 0)).Single().Path);
            Assert.Equal(multipleTags[1].Path, (await _tagManager.GetTagsAsync(1, 1)).Single().Path);

            // QIDO
            DicomWebAsyncEnumerableResponse<DicomDataset> queryResponse = await _client.QueryInstancesAsync($"{weightTag.GetPath()}={50}");
            DicomDataset[] instances = await queryResponse.ToArrayAsync();
            Assert.Contains(instances, instance => instance.ToInstanceIdentifier().Equals(instance2.ToInstanceIdentifier()));
        }

        [Fact]
        public async Task GivenExtendedQueryTagWithErrors_WhenReindexing_ThenShouldSucceedWithErrors()
        {
            if (_isUsingInProcTestServer)
            {
                // AzureFunction doesn't have InProc test sever, skip this test.
                return;
            }

            // Define tags
            DicomTag tag = DicomTag.PatientAge;

            // Define DICOM files
            DicomDataset instance1 = Samples.CreateRandomInstanceDataset();
            DicomDataset instance2 = Samples.CreateRandomInstanceDataset();
            DicomDataset instance3 = Samples.CreateRandomInstanceDataset();

            // Annotate files
            // (Disable Auto-validate)
            instance1.NotValidated();
            instance2.NotValidated();

            instance1.Add(tag, "foobar");
            instance2.Add(tag, "invalid");
            instance3.Add(tag, "053Y");

            // Upload files (with a few errors)
            await _instanceManager.StoreAsync(new DicomFile(instance1));
            await _instanceManager.StoreAsync(new DicomFile(instance2));
            await _instanceManager.StoreAsync(new DicomFile(instance3));

            // Add extended query tags
            var operationStatus = await _tagManager.AddTagsAsync(
                new AddExtendedQueryTagEntry[]
                {
                    new AddExtendedQueryTagEntry { Path = tag.GetPath(), VR = tag.GetDefaultVR().Code, Level = QueryTagLevel.Instance },
                });
            Assert.Equal(OperationRuntimeStatus.Completed, operationStatus.Status);

            // Check specific tag
            GetExtendedQueryTagEntry actual = await _tagManager.GetTagAsync(tag.GetPath());
            Assert.Equal(tag.GetPath(), actual.Path);
            Assert.Equal(2, actual.Errors.Count);

            // Query Errors
            var errors = await _tagManager.GetTagErrorsAsync(tag.GetPath(), 2, 0);
            Assert.Equal(2, errors.Count);

            Assert.Equal(errors[0].ErrorMessage, (await _tagManager.GetTagErrorsAsync(tag.GetPath(), 1, 0)).Single().ErrorMessage);
            Assert.Equal(errors[1].ErrorMessage, (await _tagManager.GetTagErrorsAsync(tag.GetPath(), 1, 1)).Single().ErrorMessage);
        }


        [Fact(Skip = "Pending on QIDO to respect QueryStatus")]
        public async Task GivenExtendedQueryTagWithErrors_WhenUpdateQueryStatus_ThenQidoResultShouldChangeRespectively()
        {
            if (_isUsingInProcTestServer)
            {
                // AzureFunction doesn't have InProc test sever, skip this test.
                return;
            }

            // Define tags
            DicomTag tag = DicomTag.PatientAge;

            // Define DICOM files
            DicomDataset instance1 = Samples.CreateRandomInstanceDataset();
            DicomDataset instance2 = Samples.CreateRandomInstanceDataset();
            DicomDataset instance3 = Samples.CreateRandomInstanceDataset();

            // Annotate files
            // (Disable Auto-validate)
            instance1.NotValidated();
            instance2.NotValidated();

            instance1.Add(tag, "foobar");
            instance2.Add(tag, "invalid");
            instance3.Add(tag, "053Y");

            // Upload files (with a few errors)
            await _instanceManager.StoreAsync(new DicomFile(instance1));
            await _instanceManager.StoreAsync(new DicomFile(instance2));
            await _instanceManager.StoreAsync(new DicomFile(instance3));

            // Add extended query tags
            var operationStatus = await _tagManager.AddTagsAsync(
                new AddExtendedQueryTagEntry[]
                {
                    new AddExtendedQueryTagEntry { Path = tag.GetPath(), VR = tag.GetDefaultVR().Code, Level = QueryTagLevel.Instance },
                });
            Assert.Equal(OperationRuntimeStatus.Completed, operationStatus.Status);

            // It should be disabled by default
            GetExtendedQueryTagEntry actual = await _tagManager.GetTagAsync(tag.GetPath());
            Assert.Equal(QueryStatus.Disabled, actual.QueryStatus);

            // TODO: QIDO should throw exception

            // Enable
            actual = await _tagManager.UpdateExtendedQueryTagAsync(tag.GetPath(), new UpdateExtendedQueryTagEntry() { QueryStatus = QueryStatus.Disabled });
            Assert.Equal(QueryStatus.Enabled, actual.QueryStatus);

            // TODO: QIDO should not throw exception, but erronous tags are in header
        }



        [Theory]
        [MemberData(nameof(GetRequestBodyWithMissingProperty))]
        public async Task GivenMissingPropertyInRequestBody_WhenCallingPostAsync_ThenShouldThrowException(string requestBody, string missingProperty)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{DicomApiVersions.Latest}/extendedquerytags");
            {
                request.Content = new StringContent(requestBody);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(DicomWebConstants.ApplicationJsonMediaType);
            }

            HttpResponseMessage response = await _client.HttpClient.SendAsync(request, default(CancellationToken))
                .ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(string.Format("The request body is not valid: [0].{0} - The Dicom Tag Property {0} must be specified and must not be null, empty or whitespace", missingProperty), response.Content.ReadAsStringAsync().Result);
        }

        [Fact]
        public async Task GivenInvalidTagLevelInRequestBody_WhenCallingPostAync_ThenShouldThrowException()
        {
            string requestBody = "[{\"Path\":\"00100040\",\"Level\":\"Studys\"}]";
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{DicomApiVersions.Latest}/extendedquerytags");
            {
                request.Content = new StringContent(requestBody);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(DicomWebConstants.ApplicationJsonMediaType);
            }

            HttpResponseMessage response = await _client.HttpClient.SendAsync(request, default(CancellationToken)).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("The request body is not valid: [0].Level - Input Dicom Tag Level 'Studys' is invalid. It must have value 'Study', 'Series' or 'Instance'.", response.Content.ReadAsStringAsync().Result);
        }

        public static IEnumerable<object[]> GetRequestBodyWithMissingProperty
        {
            get
            {
                yield return new object[] { "[{\"Path\":\"00100040\"}]", "Level" };
                yield return new object[] { "[{\"Path\":\"\",\"Level\":\"Study\"}]", "Path" };
            }
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _tagManager.DisposeAsync();
            await _instanceManager.DisposeAsync();
        }
    }
}
