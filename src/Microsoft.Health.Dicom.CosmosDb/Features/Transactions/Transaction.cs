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

        public Transaction(
            IDocumentClient client,
            string databaseId,
            string collectionId,
            RequestOptions requestOptions = null)
        {
            _client = EnsureArg.IsNotNull(client);
            _databaseId = EnsureArg.IsNotNullOrWhiteSpace(databaseId);
            _collectionId = EnsureArg.IsNotNullOrWhiteSpace(collectionId);

            _requestOptions = requestOptions;
        }

        public Guid TransactionId { get; } = Guid.NewGuid();

        public void Dispose() => Dispose(true);

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Uri storedProcedureUri = UriFactory.CreateStoredProcedureUri(_databaseId, _collectionId, StoredProcedureId);
            await _client.ExecuteStoredProcedureAsync<dynamic>(storedProcedureUri, _requestOptions, cancellationToken, _transactionItems);

            _transactionItems.Clear();
        }

        public void Abort()
            => _transactionItems.Clear();

        public void DeleteDocument(string documentId, string documentETag)
        {
            EnsureArg.IsNotNullOrWhiteSpace(documentId);
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
