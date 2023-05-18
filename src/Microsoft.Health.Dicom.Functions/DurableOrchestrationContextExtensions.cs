// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Microsoft.Health.Dicom.Functions;

internal static class DurableOrchestrationContextExtensions
{
    private static readonly Func<IDurableOrchestrationContext, string> GetExecutionIdFunc = CreateGetExecutionIdFunc();

    public static string GetExecutionId(this IDurableOrchestrationContext context)
        => GetExecutionIdFunc(context);

    private static Func<IDurableOrchestrationContext, string> CreateGetExecutionIdFunc()
    {
        Type contextType = typeof(IDurableOrchestrationContext).Assembly.GetType("Microsoft.Azure.WebJobs.Extensions.DurableTask.DurableOrchestrationContext");
        PropertyInfo executionIdProperty = contextType.GetProperty("ExecutionId", BindingFlags.NonPublic | BindingFlags.Instance);

        // Build an expression that can be compiled into IL and loaded into the app domain
        ParameterExpression param = Expression.Parameter(typeof(IDurableOrchestrationContext), "context");
        Expression body = Expression.Property(Expression.Convert(param, contextType), executionIdProperty);
        return Expression.Lambda<Func<IDurableOrchestrationContext, string>>(body, param).Compile();
    }
}
