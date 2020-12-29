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
        /// <summary>
        /// Check to make sure Table Storage is set up and is working properly
        /// </summary>
        /// <param name="client">Cloud table client to use in the test</param>
        /// <param name="configuration"> Configuration for the table data store</param>
        /// <param name="cancellationToken"> Cancellation Token</param>
        /// <returns>A <see cref="Task"/>.</returns>
        Task PerformTestAsync(CloudTableClient client, TableDataStoreConfiguration configuration, CancellationToken cancellationToken = default);
    }
}
