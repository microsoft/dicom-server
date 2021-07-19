// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Functions.Indexing;
using Microsoft.Health.Dicom.Functions.Indexing.Models;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Indexing
{
    public partial class ReindexDurableFunctionTests
    {
        [Fact]
        public async Task GivenNoInput_WhenStartingToReindexInstances_ThenReturnBadRequest()
        {
            HttpResponseMessage actual;
            IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();

            // Null
            actual = await _reindexDurableFunction.StartReindexingInstancesAsync(
                CreateRequest(null),
                client,
                NullLogger.Instance);

            Assert.Equal(HttpStatusCode.BadRequest, actual.StatusCode);

            // Empty
            actual = await _reindexDurableFunction.StartReindexingInstancesAsync(
                CreateRequest(new List<int>()),
                client,
                NullLogger.Instance);

            Assert.Equal(HttpStatusCode.BadRequest, actual.StatusCode);
        }

        [Fact]
        public async Task GivenExtendedQueryTagKeys_WhenStartingToReindexInstances_ThenReturnOperationId()
        {
            string instanceId = Guid.NewGuid().ToString();
            var expectedTagKeys = new List<int> { 1, 2, 3 };
            IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();

            client
                .StartNewAsync(
                    nameof(ReindexDurableFunction.ReindexInstancesAsync),
                    Arg.Is<ReindexInput>(x => x.QueryTagKeys.SequenceEqual(expectedTagKeys)))
                .Returns(Task.FromResult(instanceId));

            HttpResponseMessage response = await _reindexDurableFunction.StartReindexingInstancesAsync(
                CreateRequest(expectedTagKeys),
                client,
                NullLogger.Instance);

            Assert.Equal(instanceId, await response.Content.ReadAsStringAsync());
            await client
                .Received(1)
                .StartNewAsync(
                    nameof(ReindexDurableFunction.ReindexInstancesAsync),
                    Arg.Is<ReindexInput>(x => x.QueryTagKeys.SequenceEqual(expectedTagKeys)));
        }

        private static HttpRequestMessage CreateRequest(IReadOnlyCollection<int> tagKeys)
            => new HttpRequestMessage(HttpMethod.Post, new Uri("https://functions.dicom/unit/test/reindexing"))
            {
                Content = new StringContent(JsonConvert.SerializeObject(tagKeys), Encoding.UTF8, MediaTypeNames.Application.Json)
            };
    }
}
