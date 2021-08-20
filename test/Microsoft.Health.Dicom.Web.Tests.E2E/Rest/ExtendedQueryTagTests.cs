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
using Microsoft.Health.Dicom.Core.Features.Model;
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

        public ExtendedQueryTagTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            EnsureArg.IsNotNull(fixture.Client, nameof(fixture.Client));
            _client = fixture.Client;
            _tagManager = new DicomTagsManager(_client);
            _instanceManager = new DicomInstancesManager(_client);
        }

        [Fact(Skip = "Skip until test environment setup completes.")]
        public async Task GivenValidExtendedQueryTag_WhenGoThroughEndToEndScenario_ThenShouldSucceed()
        {
            DicomTag tag = DicomTag.SeriesNumber;

            // upload file
            string tagValue = "123";
            DicomDataset dataset = Samples.CreateRandomInstanceDataset();
            dataset.Add(tag, tagValue);
            await _instanceManager.StoreAsync(new DicomFile(dataset));
            InstanceIdentifier instanceId = dataset.ToInstanceIdentifier();

            // add tag
            AddExtendedQueryTagEntry addExtendedQueryTagEntry = new AddExtendedQueryTagEntry { Path = tag.GetPath(), VR = tag.GetDefaultVR().Code, Level = QueryTagLevel.Instance };
            var operationStatus = await _tagManager.AddTagsAsync(new[] { addExtendedQueryTagEntry });
            Assert.Equal(OperationRuntimeStatus.Completed, operationStatus.Status);

            // QIDO
            // Query on series for standardTagSeries
            DicomWebAsyncEnumerableResponse<DicomDataset> queryResponse = await _client.QueryInstancesAsync($"{tag.GetPath()}={tagValue}", cancellationToken: default);
            DicomDataset[] instances = await queryResponse.ToArrayAsync();
            Assert.Contains(instances, instance => instance.ToInstanceIdentifier().Equals(instanceId));
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
            Assert.Contains(string.Format("The request body is not valid. Details: \r\nThe Dicom Tag Property {0} must be specified and must not be null, empty or whitespace", missingProperty), response.Content.ReadAsStringAsync().Result);
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

            HttpResponseMessage response = await _client.HttpClient.SendAsync(request, default(CancellationToken))
                .ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("The request body is not valid. Details: \r\nInput Dicom Tag Level 'Studys' is invalid. It must have value 'Study', 'Series' or 'Instance'.", response.Content.ReadAsStringAsync().Result);
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
