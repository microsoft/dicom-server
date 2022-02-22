// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Operations.DurableTask
{
    public interface IOrchestrationManager : IOrchestrationManager<object>
    { }

    public interface IOrchestrationManager<T>
    {
        Task<string> StartAsync(OrchestrationRequest<T> request);
    }
}
