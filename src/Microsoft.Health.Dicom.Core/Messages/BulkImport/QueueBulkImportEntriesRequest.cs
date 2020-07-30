// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.BulkImport;

namespace Microsoft.Health.Dicom.Core.Messages.BulkImport
{
    public class QueueBulkImportEntriesRequest : IRequest<QueueBulkImportEntriesResponse>
    {
        public QueueBulkImportEntriesRequest(string accountName, IReadOnlyList<BlobReference> blobReferences)
        {
            AccountName = accountName;
            BlobReferences = blobReferences;
        }

        public string AccountName { get; }

        public IReadOnlyList<BlobReference> BlobReferences { get; }
    }
}
