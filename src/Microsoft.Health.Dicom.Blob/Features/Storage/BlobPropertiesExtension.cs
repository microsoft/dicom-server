// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage.Blobs.Models;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;

namespace Microsoft.Health.Dicom.Blob.Features.Storage;

public static class BlobPropertiesExtension
{
    public static FileProperties ToFileProperties(this BlobProperties blobProperties, string path)
    {
        EnsureArg.IsNotNull(blobProperties, nameof(blobProperties));
        return new FileProperties
        {
            ContentLength = blobProperties.ContentLength,
            ETag = blobProperties.ETag.ToString(),
            Path = path,
        };
    }
}
