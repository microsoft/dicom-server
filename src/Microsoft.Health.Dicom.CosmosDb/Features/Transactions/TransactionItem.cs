// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Transactions
{
    internal class TransactionItem
    {
        private TransactionItem(Operation operation, Uri documentLink, string documentETag)
        {
            EnsureArg.IsNotNull(documentLink, nameof(documentLink));
            EnsureArg.IsNotEmptyOrWhitespace(documentETag, nameof(documentETag));

            Operation = operation;
            DocumentETag = documentETag;
            DocumentLink = documentLink;
        }

        public Operation Operation { get; }

        public string DocumentETag { get; }

        public Uri DocumentLink { get; }

        public static TransactionItem CreateDeleteTransactionItem(Uri documentLink, string documentETag)
            => new TransactionItem(Operation.Delete, documentLink, documentETag);
    }
}
