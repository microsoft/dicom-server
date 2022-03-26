// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Microsoft.Health.Functions.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

public class WebJobsIntegrationTestFixture<TWebStartup, TFunctionsStartup> : HttpIntegrationTestFixture<TWebStartup>, IAsyncLifetime
    where TFunctionsStartup : FunctionsStartup, new()
{
    private readonly IHost _jobHost;

    public WebJobsIntegrationTestFixture(IMessageSink sink)
        => _jobHost = IsInProcess
            ? AzureFunctionsJobHostBuilder
                .Create<TFunctionsStartup>()
                .ConfigureLogging(b => b.AddXUnit(sink))
                .ConfigureWebJobs(b => b.AddDurableTask())
                .Build()
            : NullHost.Instance;

    public async Task DisposeAsync()
    {
        await _jobHost.StopAsync();
        Dispose();
    }

    public Task InitializeAsync()
        => _jobHost.StartAsync();
}
