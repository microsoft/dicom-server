// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Transactions
{
    internal static class TransactionDocumentClientExtensions
    {
        public const string TransactionProperty = "_isTransaction";
        private const string StoredProcedureBodyFileName = "Features\\Transactions\\TransactionStoredProcedure.js";

        public static ITransaction CreateTransaction(this IDocumentClient documentClient, string databaseId, string collectionId, RequestOptions requestOptions)
        {
            EnsureArg.IsNotNull(documentClient);
            return new Transaction(documentClient, databaseId, collectionId, requestOptions);
        }

        public static async Task RegisterTransactionCapabilityAsync(
            this IDocumentClient documentClient, string databaseId, string collectionId)
        {
            EnsureArg.IsNotNull(documentClient);
            EnsureArg.IsNotNullOrWhiteSpace(databaseId);
            EnsureArg.IsNotNullOrWhiteSpace(collectionId);

            Uri storedProcedureUri = UriFactory.CreateStoredProcedureUri(databaseId, collectionId, Transaction.StoredProcedureId);
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);

            var storedProcedure = new StoredProcedure()
            {
                Id = Transaction.StoredProcedureId,
                Body = File.ReadAllText(StoredProcedureBodyFileName),
            };

            try
            {
                await documentClient.CreateStoredProcedureAsync(collectionUri, storedProcedure);
            }
            catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                await documentClient.ReplaceStoredProcedureAsync(storedProcedureUri, storedProcedure);
            }
        }
    }
}
