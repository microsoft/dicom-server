// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Dicom.Core.Features.Telemetry;

namespace Microsoft.Health.Dicom.Functions.Registration;

public class ReplaySafeUpdateMeter
{
    private readonly IDurableOrchestrationContext _context;

    private readonly UpdateMeter _updateMeter;

    internal ReplaySafeUpdateMeter(IDurableOrchestrationContext context, UpdateMeter updateMeter)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsNotNull(updateMeter, nameof(updateMeter));

        _context = context;
        _updateMeter = updateMeter;
    }

    public void Add(int UpdatedInstances)
    {
        if (!_context.IsReplaying)
        {
            _updateMeter.UpdatedInstances.Add(UpdatedInstances,
                new KeyValuePair<string, object>("ExecutionId", _context.NewGuid()));
        }
    }
}
