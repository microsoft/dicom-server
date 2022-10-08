// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.AzureStorage;
using DurableTask.Core;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Health.Dicom.Functions.Client.HealthChecks;

internal sealed class DurableTaskHealthCheck : IHealthCheck
{
    private readonly string _taskHubName;
    private readonly CloudBlobClient _blobClient;
    private readonly CloudQueueClient _queueClient;
    private readonly CloudTableClient _tableClient;
    private readonly ILogger _logger;

    // TODO: Replace this reflection-based approached with a first-class API from the DurableTask library
    private readonly static Func<IDurableClient, CloudBlobClient> GetBlobClient = GetBodyClientAccessor();
    private readonly static Func<IDurableClient, CloudQueueClient> GetQueueClient = GetQueueClientAccessor();
    private readonly static Func<IDurableClient, CloudTableClient> GetTableClient = GetTableClientAccessor();

    public DurableTaskHealthCheck(IDurableClientFactory factory, ILogger<DurableTaskHealthCheck> logger)
        : this(EnsureArg.IsNotNull(factory, nameof(factory)).CreateClient(), logger)
    {
    }

    internal DurableTaskHealthCheck(IDurableClient client, ILogger<DurableTaskHealthCheck> logger)
        : this(client.TaskHubName, GetBlobClient(client), GetQueueClient(client), GetTableClient(client), logger)
    {
    }

    internal DurableTaskHealthCheck(string name, CloudBlobClient blobClient, CloudQueueClient queueClient, CloudTableClient tableClient, ILogger<DurableTaskHealthCheck> logger)
    {
        _taskHubName = name;
        _blobClient = EnsureArg.IsNotNull(blobClient, nameof(blobClient));
        _queueClient = EnsureArg.IsNotNull(queueClient, nameof(queueClient));
        _tableClient = EnsureArg.IsNotNull(tableClient, nameof(tableClient));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(context, nameof(context));

        await AssertTaskHubConnectionAsync(cancellationToken);

        _logger.LogInformation("Successfully connected to the Durable TaskHub '{Name}.'", _taskHubName);
        return HealthCheckResult.Healthy();
    }

    private async Task AssertTaskHubConnectionAsync(CancellationToken cancellationToken)
    {
        await _blobClient.GetServicePropertiesAsync(null, null, cancellationToken: cancellationToken);
        await _queueClient.GetServicePropertiesAsync(null, null, cancellationToken: cancellationToken);
        await _tableClient.GetServicePropertiesAsync(null, null, cancellationToken: cancellationToken);
    }

    private static Func<IDurableClient, CloudBlobClient> GetBodyClientAccessor()
    {
        FieldInfo blobClientField = typeof(AzureStorageOrchestrationService).Assembly
            .GetType("DurableTask.AzureStorage.Storage.AzureStorageClient")
            .GetField("blobClient", BindingFlags.NonPublic | BindingFlags.Instance);

        ParameterExpression param = Expression.Parameter(typeof(IDurableClient), "client");
        Expression body = Expression.MakeMemberAccess(GetAzureStorageClientExpression(param), blobClientField);
        return Expression.Lambda<Func<IDurableClient, CloudBlobClient>>(body, param).Compile();
    }

    private static Func<IDurableClient, CloudQueueClient> GetQueueClientAccessor()
    {
        FieldInfo queueClientField = typeof(AzureStorageOrchestrationService).Assembly
            .GetType("DurableTask.AzureStorage.Storage.AzureStorageClient")
            .GetField("queueClient", BindingFlags.NonPublic | BindingFlags.Instance);

        ParameterExpression param = Expression.Parameter(typeof(IDurableClient), "client");
        Expression body = Expression.MakeMemberAccess(GetAzureStorageClientExpression(param), queueClientField);
        return Expression.Lambda<Func<IDurableClient, CloudQueueClient>>(body, param).Compile();
    }

    private static Func<IDurableClient, CloudTableClient> GetTableClientAccessor()
    {
        FieldInfo tableClientField = typeof(AzureStorageOrchestrationService).Assembly
            .GetType("DurableTask.AzureStorage.Storage.AzureStorageClient")
            .GetField("tableClient", BindingFlags.NonPublic | BindingFlags.Instance);

        ParameterExpression param = Expression.Parameter(typeof(IDurableClient), "client");
        Expression body = Expression.MakeMemberAccess(GetAzureStorageClientExpression(param), tableClientField);
        return Expression.Lambda<Func<IDurableClient, CloudTableClient>>(body, param).Compile();
    }

    private static Expression GetAzureStorageClientExpression(ParameterExpression param)
    {
        Type clientType = typeof(IDurableClient).Assembly.GetType("Microsoft.Azure.WebJobs.Extensions.DurableTask.DurableClient");
        FieldInfo clientField = clientType.GetField("client", BindingFlags.NonPublic | BindingFlags.Instance);
        PropertyInfo propertyInfo = typeof(TaskHubClient).GetProperty(nameof(TaskHubClient.ServiceClient));
        FieldInfo storageClientField = typeof(AzureStorageOrchestrationService).GetField("azureStorageClient", BindingFlags.NonPublic | BindingFlags.Instance);

        return Expression.MakeMemberAccess(
            Expression.Convert(
                Expression.MakeMemberAccess(
                Expression.MakeMemberAccess(Expression.Convert(param, clientType), clientField),
                propertyInfo),
                typeof(AzureStorageOrchestrationService)),
            storageClientField);
    }
}
