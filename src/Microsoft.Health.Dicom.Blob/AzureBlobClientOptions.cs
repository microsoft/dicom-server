// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage.Blobs;

namespace Microsoft.Health.Dicom.Blob;

/// <inheritdoc cref="BlobClientOptions" />
public sealed class AzureBlobClientOptions : BlobClientOptions
{
    // This class is a workaround for using IOptions as the BlobClientOptions ctor isn't used
}
