// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.Metrics;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.Registration;

public class ReplaySafeCounter
{
    private readonly IDurableOrchestrationContext _context;

    private readonly Counter<int> _counter;

    internal ReplaySafeCounter(IDurableOrchestrationContext context, Counter<int> counter)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsNotNull(counter, nameof(counter));

        _context = context;
        _counter = counter;
    }

    public void Add(int count)
    {
        if (!_context.IsReplaying)
        {
            _counter.Add(count);
        }
    }
}
