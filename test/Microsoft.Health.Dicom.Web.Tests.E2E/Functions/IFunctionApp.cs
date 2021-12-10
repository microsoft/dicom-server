// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Functions
{
    public interface IFunctionApp
    {
        ValueTask<JobHostExecution> StartAsync();
    }
}
