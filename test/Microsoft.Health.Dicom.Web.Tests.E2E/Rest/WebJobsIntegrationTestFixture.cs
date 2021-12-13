// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class WebJobsIntegrationTestFixture<T> : HttpIntegrationTestFixture<T>, IAsyncLifetime
    {
        public async Task DisposeAsync()
        {
            await TestDicomWebServer.WebJobsHost.StopAsync();
            Dispose();
        }

        public Task InitializeAsync()
            => TestDicomWebServer.WebJobsHost.StartAsync();
    }
}
