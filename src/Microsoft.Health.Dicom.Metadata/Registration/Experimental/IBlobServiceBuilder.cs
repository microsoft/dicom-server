// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Blob.Configs;

namespace Microsoft.Extensions.DependencyInjection
{
    public interface IBlobServiceBuilder
    {
        IServiceCollection Services { get; }

        BlobDataStoreConfiguration Configuration { get; }
    }
}
