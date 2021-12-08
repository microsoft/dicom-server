// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Health.DicomCast.TableStorage.Configs;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage
{
    /// <summary>
    /// Provides methods for creating a <see cref="TableServiceClient"/> instance and initializing tables.
    /// </summary>
    public interface ITableServiceClientInitializer
    {
        /// <summary>
        /// Creates <see cref="TableServiceClient"/> based on the given <see cref="TableDataStoreConfiguration"/>.
        /// </summary>
        /// <returns>A <see cref="TableServiceClient"/> instance</returns>
        TableServiceClient CreateTableServiceClient();

        /// <summary>
        /// Initialize table data store
        /// </summary>
        /// <param name="tableServiceClient">The <see cref="TableServiceClient"/> instance to use for initialization.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        Task InitializeDataStoreAsync(TableServiceClient tableServiceClient);
    }
}
