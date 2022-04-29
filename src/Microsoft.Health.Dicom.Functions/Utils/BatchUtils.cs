// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Functions.Utils;
internal static class BatchUtils
{
    public static async Task ExecuteBatchAsync(IReadOnlyList<VersionedInstanceIdentifier> instanceIdentifiers, int threadCount, Func<VersionedInstanceIdentifier, Task> taskCreator)
    {
        EnsureArg.IsNotNull(instanceIdentifiers, nameof(instanceIdentifiers));
        EnsureArg.IsGt(threadCount, 0);
        EnsureArg.IsNotNull(taskCreator);
        for (int i = 0; i < instanceIdentifiers.Count; i += threadCount)
        {
            var tasks = new List<Task>();
            for (int j = i; j < Math.Min(instanceIdentifiers.Count, i + threadCount); j++)
            {
                tasks.Add(taskCreator(instanceIdentifiers[j]));
            }

            await Task.WhenAll(tasks);
        }
    }
}
