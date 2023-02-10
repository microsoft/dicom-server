// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using EnsureThat;

namespace Microsoft.Health.Dicom.Functions.Client.TaskHub;

internal abstract class TrackingTable
{
    private readonly TableClient _tableClient;

    protected TrackingTable(TableServiceClient queueServiceClient, string tableName)
        => _tableClient = EnsureArg.IsNotNull(queueServiceClient, nameof(queueServiceClient)).GetTableClient(tableName);

    public string Name => _tableClient.Name;

    public virtual async ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        // Note: There is no ExistsAsync method for TableClient, so instead
        //       we'll run a query that will (probably) not return any results
        AsyncPageable<TableEntity> pageable = _tableClient.QueryAsync<TableEntity>(
            filter: "PartitionKey eq ''",
            maxPerPage: 1,
            cancellationToken: cancellationToken);

        try
        {
            await pageable.GetAsyncEnumerator(cancellationToken).MoveNextAsync();
            return true;
        }
        catch (RequestFailedException rfe) when (rfe.Status == (int)HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
