// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Health.DicomCast.TableStorage.Configs;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage
{
    public interface ITableClientTestProvider
    {
        Task PerformTestAsync(CloudTableClient client, TableDataStoreConfiguration configuration, TableConfiguration tableConfiguration, CancellationToken cancellationToken = default);
    }
}
