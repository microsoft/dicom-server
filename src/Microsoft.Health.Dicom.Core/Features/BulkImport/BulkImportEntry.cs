// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Features.BulkImport
{
    public class BulkImportEntry
    {
        public BulkImportEntry(long sequence, string accountName, DateTimeOffset timestamp, string containerName, string blobName)
        {
            Sequence = sequence;
            AccountName = accountName;
            Timestamp = timestamp;
            ContainerName = containerName;
            BlobName = blobName;
        }

        public long Sequence { get; }

        public string AccountName { get; }

        public DateTimeOffset Timestamp { get; }

        public string ContainerName { get; }

        public string BlobName { get; }
    }
}
