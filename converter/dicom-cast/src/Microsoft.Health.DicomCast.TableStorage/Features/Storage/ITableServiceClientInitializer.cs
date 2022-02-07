// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage
{
    /// <summary>
    /// Provides methods for creating a <see cref="TableServiceClient"/> instance and initializing tables.
    /// </summary>
    public interface ITableServiceClientInitializer
    {
        /// <summary>
        /// Initialize table data store
        /// </summary>
        /// <param name="tableServiceClient">The <see cref="TableServiceClient"/> instance to use for initialization.</param>
        /// <param name="tableList">The tableName set with fulltableNames .</param>
        /// <returns>A <see cref="Task"/>.</returns>
        Task InitializeDataStoreAsync(TableServiceClient tableServiceClient, Dictionary<string, string> tableList);
    }
}
