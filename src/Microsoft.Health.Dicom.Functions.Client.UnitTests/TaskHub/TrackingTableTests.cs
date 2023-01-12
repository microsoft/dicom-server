// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.TaskHub;

public abstract class TrackingTableTests
{
    [Fact]
    public async Task GivenMissingTable_WhenCheckingExistence_ThenReturnFalse()
    {
        // Set up clients
        const string TaskHubName = "TestTaskHub";
        string tableName = GetName(TaskHubName);
        using var tokenSource = new CancellationTokenSource();

        TableServiceClient tableServiceClient = Substitute.For<TableServiceClient>("UseDevelopmentStorage=true");
        TableClient tableClient = Substitute.For<TableClient>("UseDevelopmentStorage=true", tableName);
        AsyncPageable<TableEntity> asyncPageable = Substitute.For<AsyncPageable<TableEntity>>();
        IAsyncEnumerator<TableEntity> asyncEnumerator = Substitute.For<IAsyncEnumerator<TableEntity>>();

        tableServiceClient.GetTableClient(tableName).Returns(tableClient);
        tableClient.QueryAsync<TableEntity>("false", 1, null, tokenSource.Token).Returns(asyncPageable);
        asyncPageable.GetAsyncEnumerator(tokenSource.Token).Returns(asyncEnumerator);
        asyncEnumerator
            .MoveNextAsync(tokenSource.Token)
            .Returns(info => ValueTask.FromException<bool>(new RequestFailedException((int)HttpStatusCode.NotFound, "Cannot find table")));

        // Test
        Assert.False(await ExistsAsync(tableServiceClient, TaskHubName, tokenSource.Token));

        tableServiceClient.Received(1).GetTableClient(tableName);
        tableClient.Received(1).QueryAsync<TableEntity>("false", 1, null, tokenSource.Token);
        asyncPageable.Received(1).GetAsyncEnumerator(tokenSource.Token);
        await asyncEnumerator.Received(1).MoveNextAsync(tokenSource.Token);
    }

    [Fact]
    public async Task GivenAvailableTable_WhenCheckingExistence_ThenReturnTrue()
    {
        // Set up clients
        const string TaskHubName = "TestTaskHub";
        string tableName = GetName(TaskHubName);
        using var tokenSource = new CancellationTokenSource();

        TableServiceClient tableServiceClient = Substitute.For<TableServiceClient>("UseDevelopmentStorage=true");
        TableClient tableClient = Substitute.For<TableClient>("UseDevelopmentStorage=true", tableName);
        AsyncPageable<TableEntity> asyncPageable = Substitute.For<AsyncPageable<TableEntity>>();
        IAsyncEnumerator<TableEntity> asyncEnumerator = Substitute.For<IAsyncEnumerator<TableEntity>>();

        tableServiceClient.GetTableClient(tableName).Returns(tableClient);
        tableClient.QueryAsync<TableEntity>("false", 1, null, tokenSource.Token).Returns(asyncPageable);
        asyncPageable.GetAsyncEnumerator(tokenSource.Token).Returns(asyncEnumerator);
        asyncEnumerator
            .MoveNextAsync(tokenSource.Token)
            .Returns(info => ValueTask.FromResult(false));

        // Test
        Assert.True(await ExistsAsync(tableServiceClient, TaskHubName, tokenSource.Token));

        tableServiceClient.Received(1).GetTableClient(tableName);
        tableClient.Received(1).QueryAsync<TableEntity>("false", 1, null, tokenSource.Token);
        asyncPageable.Received(1).GetAsyncEnumerator(tokenSource.Token);
        await asyncEnumerator.Received(1).MoveNextAsync(tokenSource.Token);
    }

    protected abstract ValueTask<bool> ExistsAsync(TableServiceClient tableServiceClient, string tableName, CancellationToken cancellationToken);

    protected abstract string GetName(string taskHubName);
}
