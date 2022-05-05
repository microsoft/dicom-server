// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Dicom.Functions.Utils;

internal static class TaskBatch
{
    /// <summary>
    /// Run a batch of batch and wait for completion.
    /// </summary>
    /// <typeparam name="T">The input type.</typeparam>
    /// <param name="input">The input</param>
    /// <param name="taskFactory">The task factory.</param>
    /// <param name="threadCount">The max thread count. -1 means no limit.</param>
    /// <returns>The task.</returns>
    public static async Task RunAsync<T>(IReadOnlyList<T> input, Func<T, Task> taskFactory, int threadCount = -1)
    {
        EnsureArg.IsNotNull(input, nameof(input));
        EnsureArg.IsGt(threadCount, 0);
        EnsureArg.IsNotNull(taskFactory);
        if (threadCount == -1)
        {
            await Task.WhenAll(input.Select(x => taskFactory(x)));
        }
        else
        {
            for (int i = 0; i < input.Count; i += threadCount)
            {
                var tasks = new List<Task>();
                for (int j = i; j < Math.Min(input.Count, i + threadCount); j++)
                {
                    tasks.Add(taskFactory(input[j]));
                }

                await Task.WhenAll(tasks);
            }
        }
    }
}
