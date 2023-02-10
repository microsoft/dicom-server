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
    private readonly string _tableName;
    private readonly TableServiceClient _tableServiceClient = Substitute.For<TableServiceClient>("UseDevelopmentStorage=true");
    private readonly TableClient _tableClient;
    private readonly AsyncPageable<TableEntity> _asyncPageable = Substitute.For<AsyncPageable<TableEntity>>();
    private readonly IAsyncEnumerator<TableEntity> _asyncEnumerator = Substitute.For<IAsyncEnumerator<TableEntity>>();

    private const string EmptyQuery = "PartitionKey eq ''";
    private const string TaskHubName = "TestTaskHub";

    protected TrackingTableTests()
    {
        _tableName = GetName(TaskHubName);
        _tableClient = Substitute.For<TableClient>("UseDevelopmentStorage=true", _tableName);
        _tableServiceClient.GetTableClient(_tableName).Returns(_tableClient);
    }

    [Fact]
    public async Task GivenMissingTable_WhenCheckingExistence_ThenReturnFalse()
    {
        // Set up clients
        using var tokenSource = new CancellationTokenSource();

        _tableClient.QueryAsync<TableEntity>(EmptyQuery, 1, null, tokenSource.Token).Returns(_asyncPageable);
        _asyncPageable.GetAsyncEnumerator(tokenSource.Token).Returns(_asyncEnumerator);
        _asyncEnumerator
            .MoveNextAsync(tokenSource.Token)
            .Returns(info => ValueTask.FromException<bool>(new RequestFailedException((int)HttpStatusCode.NotFound, "Cannot find table")));

        // Test
        Assert.False(await ExistsAsync(_tableServiceClient, TaskHubName, tokenSource.Token));

        _tableServiceClient.Received(1).GetTableClient(_tableName);
        _tableClient.Received(1).QueryAsync<TableEntity>(EmptyQuery, 1, null, tokenSource.Token);
        _asyncPageable.Received(1).GetAsyncEnumerator(tokenSource.Token);
        await _asyncEnumerator.Received(1).MoveNextAsync(tokenSource.Token);
    }

    [Fact]
    public async Task GivenAvailableTable_WhenCheckingExistence_ThenReturnTrue()
    {
        // Set up clients
        using var tokenSource = new CancellationTokenSource();

        _tableClient.QueryAsync<TableEntity>(EmptyQuery, 1, null, tokenSource.Token).Returns(_asyncPageable);
        _asyncPageable.GetAsyncEnumerator(tokenSource.Token).Returns(_asyncEnumerator);
        _asyncEnumerator
            .MoveNextAsync(tokenSource.Token)
            .Returns(info => ValueTask.FromResult(true));

        // Test
        Assert.True(await ExistsAsync(_tableServiceClient, TaskHubName, tokenSource.Token));

        _tableServiceClient.Received(1).GetTableClient(_tableName);
        _tableClient.Received(1).QueryAsync<TableEntity>(EmptyQuery, 1, null, tokenSource.Token);
        _asyncPageable.Received(1).GetAsyncEnumerator(tokenSource.Token);
        await _asyncEnumerator.Received(1).MoveNextAsync(tokenSource.Token);
    }

    protected abstract ValueTask<bool> ExistsAsync(TableServiceClient tableServiceClient, string tableName, CancellationToken cancellationToken);

    protected abstract string GetName(string taskHubName);
}
