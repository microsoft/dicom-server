// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Newtonsoft.Json;
using Polly;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Storage
{
    public static class DocumentClientExtensions
    {
        public static async Task<T> ThrowDataStoreException<T>(this IDocumentClient documentClient, Func<IDocumentClient, Task<T>> action, IAsyncPolicy retryPolicy = null)
        {
            EnsureArg.IsNotNull(documentClient, nameof(documentClient));
            EnsureArg.IsNotNull(action, nameof(action));

            try
            {
                if (retryPolicy != null)
                {
                    return await retryPolicy.ExecuteAsync(() => action(documentClient));
                }

                return await action(documentClient);
            }
            catch (DocumentClientException e)
            {
                throw new DataStoreException(e.StatusCode, e);
            }
        }

        public static async Task ThrowDataStoreException(this IDocumentClient documentClient, Func<IDocumentClient, Task> action, IAsyncPolicy retryPolicy = null)
        {
            EnsureArg.IsNotNull(documentClient, nameof(documentClient));
            EnsureArg.IsNotNull(action, nameof(action));

            try
            {
                if (retryPolicy != null)
                {
                    await retryPolicy.ExecuteAsync(() => action(documentClient));
                }
                else
                {
                    await action(documentClient);
                }
            }
            catch (DocumentClientException e)
            {
                throw new DataStoreException(e.StatusCode, e);
            }
        }

        public static async Task<T> GetOrCreateDocumentAsync<T>(
            this IDocumentClient documentClient,
            string databaseId,
            string collectionId,
            string documentId,
            RequestOptions requestOptions,
            T defaultValue,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(documentClient, nameof(documentClient));
            EnsureArg.IsNotNullOrWhiteSpace(databaseId, nameof(databaseId));
            EnsureArg.IsNotNullOrWhiteSpace(collectionId, nameof(collectionId));
            EnsureArg.IsNotNullOrWhiteSpace(documentId, nameof(documentId));

            try
            {
                Uri documentUri = UriFactory.CreateDocumentUri(databaseId, collectionId, documentId);
                DocumentResponse<T> response = await documentClient.ReadDocumentAsync<T>(documentUri, requestOptions, cancellationToken: cancellationToken);
                return response.Document;
            }
            catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Attempt to create the document as it does not exist.
                return await documentClient.CreateOrGetDocumentAsync(databaseId, collectionId, documentId, requestOptions, defaultValue, cancellationToken);
            }
        }

        public static async Task<T> CreateOrGetDocumentAsync<T>(
            this IDocumentClient documentClient,
            string databaseId,
            string collectionId,
            string documentId,
            RequestOptions requestOptions,
            T defaultValue,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(documentClient, nameof(documentClient));
            EnsureArg.IsNotNullOrWhiteSpace(databaseId, nameof(databaseId));
            EnsureArg.IsNotNullOrWhiteSpace(collectionId, nameof(collectionId));
            EnsureArg.IsNotNullOrWhiteSpace(documentId, nameof(documentId));

            try
            {
                Uri collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
                ResourceResponse<Document> result = await documentClient.CreateDocumentAsync(collectionUri, defaultValue, requestOptions, disableAutomaticIdGeneration: true, cancellationToken: cancellationToken);

                return JsonConvert.DeserializeObject<T>(result.Resource.ToString(), requestOptions?.JsonSerializerSettings);
            }
            catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                // Attempt to read the document as it already exists.
                return await documentClient.GetOrCreateDocumentAsync(databaseId, collectionId, documentId, requestOptions, defaultValue, cancellationToken);
            }
        }
    }
}
