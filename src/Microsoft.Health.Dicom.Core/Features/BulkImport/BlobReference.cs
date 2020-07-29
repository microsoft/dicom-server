// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.BulkImport
{
    public class BlobReference
    {
        public BlobReference(string containerName, string blobName)
        {
            ContainerName = containerName;
            BlobName = blobName;
        }

        public string ContainerName { get; }

        public string BlobName { get; }
    }
}
