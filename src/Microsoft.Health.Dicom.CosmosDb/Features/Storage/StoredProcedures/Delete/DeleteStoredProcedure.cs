// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Health.CosmosDb.Features.Storage.StoredProcedures;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage.Documents;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Storage.StoredProcedures.Delete
{
    internal class DeleteStoredProcedure : StoredProcedureBase, IDicomStoredProcedure
    {
        public Task<StoredProcedureResponse<IList<string>>> Execute(
            IDocumentClient client, string databaseId, string collectionId, string partitionKey, IReadOnlyCollection<IDocument> documents, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNullOrWhiteSpace(databaseId, nameof(databaseId));
            EnsureArg.IsNotNullOrWhiteSpace(collectionId, nameof(collectionId));
            EnsureArg.IsNotNullOrWhiteSpace(partitionKey, nameof(partitionKey));
            EnsureArg.HasItems(documents, nameof(documents));

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
            IEnumerable<DeleteItem> deleteItems = documents.Select(x => new DeleteItem(UriFactory.CreateDocumentUri(databaseId, collectionId, x.Id), x.ETag));

            return ExecuteStoredProc<IList<string>>(client, collectionUri, partitionKey, cancellationToken, deleteItems);
        }
    }
}
