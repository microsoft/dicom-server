// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Microsoft.Health.Dicom.Operations.DurableTask
{
    internal interface IOrchestrationAggregate<T>
    {
        Task<string> StartAsync(StartOrchestrationArgs<T> args);
    }
}
