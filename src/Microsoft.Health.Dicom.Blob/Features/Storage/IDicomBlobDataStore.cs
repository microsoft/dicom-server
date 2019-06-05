// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Blob.Features.Storage
{
    public interface IDicomBlobDataStore
    {
        Task<Uri> AddFileAsStreamAsync(string blobName, Stream buffer, bool overwriteIfExists = false, CancellationToken cancellationToken = default);

        Task<Stream> GetFileAsStreamAsync(string blobName, CancellationToken cancellationToken = default);

        Task DeleteFileIfExistsAsync(string blobName, CancellationToken cancellationToken = default);
    }
}
