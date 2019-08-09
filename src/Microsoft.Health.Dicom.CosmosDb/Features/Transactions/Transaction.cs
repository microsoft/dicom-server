// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Transactions
{
    internal class Transaction : ITransaction
    {
        public const string StoredProcedureId = "TransactionStoredProcedure";
        private readonly IList<TransactionItem> _transactionItems = new List<TransactionItem>();
        private readonly IDocumentClient _client;
        private readonly RequestOptions _requestOptions;
        private readonly string _databaseId;
        private readonly string _collectionId;
        private bool _disposed;

        public Transaction(IDocumentClient client, string databaseId, string collectionId, RequestOptions requestOptions)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNullOrWhiteSpace(databaseId, nameof(databaseId));
            EnsureArg.IsNotNullOrWhiteSpace(collectionId, nameof(collectionId));
            EnsureArg.IsNotNull(requestOptions, nameof(requestOptions));

            _client = client;
            _databaseId = databaseId;
            _collectionId = collectionId;
            _requestOptions = requestOptions;
        }

        public void Abort() => _transactionItems.Clear();

        public void Dispose() => Dispose(true);

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Uri storedProcedureUri = UriFactory.CreateStoredProcedureUri(_databaseId, _collectionId, StoredProcedureId);
            var data = JsonConvert.SerializeObject(_transactionItems);

            StoredProcedureResponse<dynamic> result = await _client.ExecuteStoredProcedureAsync<dynamic>(storedProcedureUri, _requestOptions, cancellationToken, data);

            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new InvalidOperationException(result.StatusCode.ToString());
            }

            _transactionItems.Clear();
        }

        public void DeleteDocument(string documentId, string documentETag)
        {
            EnsureArg.IsNotNullOrWhiteSpace(documentId, nameof(documentId));
            Uri documentUri = UriFactory.CreateDocumentUri(_databaseId, _collectionId, documentId);
            _transactionItems.Add(TransactionItem.CreateDeleteTransactionItem(documentUri, documentETag));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                Abort();
            }

            _disposed = true;
        }
    }
}
