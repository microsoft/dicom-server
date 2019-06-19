// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Health.CosmosDb.Features.Storage;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DocumentClientExtensionsTests : IClassFixture<DicomCosmosDataStoreTestsFixture>
    {
        private readonly IDocumentClient _documentClient;
        private readonly DicomCosmosDataStoreTestsFixture _fixture;

        public DocumentClientExtensionsTests(DicomCosmosDataStoreTestsFixture fixture)
        {
            _fixture = fixture;
            _documentClient = fixture.DocumentClient;
        }

        [Fact]
        public async Task GivenAnExistingDocument_OnCreateOrGet_DocumentIsFetched()
        {
            var expectedDocument = new TestDocument() { Id = Guid.NewGuid().ToString(), PartitionKey = Guid.NewGuid().ToString() };
            var requestOptions = new RequestOptions() { PartitionKey = new PartitionKey(expectedDocument.PartitionKey) };

            await _documentClient.CreateDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(_fixture.DatabaseId, _fixture.CollectionId),
                expectedDocument,
                requestOptions);

            DocumentResponse<TestDocument> readResponse = await _documentClient.ReadDocumentAsync<TestDocument>(
                                            UriFactory.CreateDocumentUri(_fixture.DatabaseId, _fixture.CollectionId, expectedDocument.Id),
                                            requestOptions);

            expectedDocument = readResponse.Document;

            TestDocument actualDocument = await _documentClient.CreateOrGetDocumentAsync(
                _fixture.DatabaseId,
                _fixture.CollectionId,
                expectedDocument.Id,
                requestOptions,
                new TestDocument() { Id = expectedDocument.Id, PartitionKey = expectedDocument.PartitionKey });

            Assert.Equal(expectedDocument.Id, actualDocument.Id);
            Assert.Equal(expectedDocument.PartitionKey, actualDocument.PartitionKey);
            Assert.Equal(expectedDocument.ETag, actualDocument.ETag);
        }

        [Fact]
        public async Task GivenANonExistentDocument_OnGetOrCreate_DocumentIsCreated()
        {
            var expectedDocument = new TestDocument() { Id = Guid.NewGuid().ToString(), PartitionKey = Guid.NewGuid().ToString() };
            var requestOptions = new RequestOptions() { PartitionKey = new PartitionKey(expectedDocument.PartitionKey) };

            TestDocument actualDocument = await _documentClient.GetorCreateDocumentAsync(
                                            _fixture.DatabaseId,
                                            _fixture.CollectionId,
                                            expectedDocument.Id,
                                            requestOptions,
                                            expectedDocument);

            Assert.Equal(expectedDocument.Id, actualDocument.Id);
            Assert.Equal(expectedDocument.PartitionKey, actualDocument.PartitionKey);
            Assert.False(string.IsNullOrWhiteSpace(actualDocument.ETag));
        }

        private class TestDocument
        {
            [JsonProperty(KnownDocumentProperties.Id)]
            public string Id { get; set; }

            [JsonProperty(KnownDocumentProperties.PartitionKey)]
            public string PartitionKey { get; set; }

            [JsonProperty(KnownDocumentProperties.ETag)]
            public string ETag { get; set; }
        }
    }
}
