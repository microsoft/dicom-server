// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Storage.StoredProcedures.Delete
{
    internal class DeleteItem
    {
        [JsonConstructor]
        public DeleteItem(Uri documentLink, string documentETag)
        {
            EnsureArg.IsNotNull(documentLink, nameof(documentLink));
            EnsureArg.IsNotEmptyOrWhitespace(documentETag, nameof(documentETag));

            DocumentETag = documentETag;
            DocumentLink = documentLink.OriginalString;
        }

        [JsonProperty("documentETag")]
        public string DocumentETag { get; }

        [JsonProperty("documentLink")]
        public string DocumentLink { get; }
    }
}
