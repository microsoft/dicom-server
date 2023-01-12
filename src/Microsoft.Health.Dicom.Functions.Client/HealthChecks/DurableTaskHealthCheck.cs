// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Health.Dicom.Functions.Client.HealthChecks;

internal sealed class DurableTaskHealthCheck : IHealthCheck
{
    public DurableTaskHealthCheck(IDurableClientFactory factory, ILogger<DurableTaskHealthCheck> logger)
        : this(EnsureArg.IsNotNull(factory, nameof(factory)).CreateClient(), logger)
    {
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(context, nameof(context));

        try
        {
            await AssertTaskHubConnectionAsync(cancellationToken);
        }
        catch (WindowsAzure.Storage.StorageException ex) when (cancellationToken.IsCancellationRequested && ex.InnerException is OperationCanceledException)
        {
            // Cancelled storage operations sometimes throw a StorageException wrapping an OperationCanceledException (or the child TaskCanceledException):
            // https://github.com/Azure/azure-storage-net/issues/512
            // The Azure Webjobs framework catches these exceptions (https://github.com/Azure/azure-webjobs-sdk/pull/1273) but we encounter this
            // case because we use the DurableTask clients directly via reflection. Here we throw the inner exception, which is expected by ASP.NET Core's DefaultHealthCheckService:
            // https://github.com/dotnet/aspnetcore/blob/9cbf44b7c192cf064a82934bbf479a93a1953c26/src/HealthChecks/HealthChecks/src/DefaultHealthCheckService.cs#L121,
            // while maintaining the stack trace of the inner exception:
            // https://learn.microsoft.com/en-us/archive/msdn-magazine/2015/november/essential-net-csharp-exception-handling#throwing-existing-exceptions-without-replacing-stack-information.
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }

        _logger.LogInformation("Successfully connected to the Durable TaskHub '{Name}.'", _taskHubName);
        return HealthCheckResult.Healthy("Successfully connected.");
    }
}
