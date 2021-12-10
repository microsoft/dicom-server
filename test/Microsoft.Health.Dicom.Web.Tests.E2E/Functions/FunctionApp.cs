// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Functions
{
    internal sealed class FunctionApp : IFunctionApp
    {
        private readonly IHost _host;

        public FunctionApp(IHost host)
            => _host = EnsureArg.IsNotNull(host, nameof(host));

        public async ValueTask<JobHostExecution> StartAsync()
        {
            await _host.StartAsync();
            return new JobHostExecution(_host.Services.GetRequiredService<IJobHost>());
        }
    }
}
