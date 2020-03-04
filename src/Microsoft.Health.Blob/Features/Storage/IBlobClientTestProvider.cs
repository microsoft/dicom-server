// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Health.Blob.Configs;

namespace Microsoft.Health.Blob.Features.Storage
{
    public interface IBlobClientTestProvider
    {
        Task PerformTestAsync(CloudBlobClient blobClient, BlobDataStoreConfiguration configuration, BlobContainerConfiguration blobContainerConfiguration, CancellationToken cancellationToken = default);
    }
}
