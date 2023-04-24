// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage.Blobs;

namespace Microsoft.Health.Dicom.Blob.Features.Storage;
public interface IBlobClient
{
    BlobContainerClient BlobContainerClient { get; }

    bool IsExternal { get; }
}
