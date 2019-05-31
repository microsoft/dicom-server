// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Blob.Features.Storage
{
    public sealed class NonDisposingScope : IScoped<CloudBlobClient>
    {
        public NonDisposingScope(CloudBlobClient value)
        {
            Value = EnsureArg.IsNotNull(value, nameof(value));
        }

        public CloudBlobClient Value { get; }

        public void Dispose()
        {
        }
    }
}
