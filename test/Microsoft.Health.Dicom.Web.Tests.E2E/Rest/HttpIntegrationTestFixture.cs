// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net.Http;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class HttpIntegrationTestFixture<TStartup> : IDisposable
    {
        public HttpIntegrationTestFixture()
            : this(Path.Combine("src"))
        {
        }

        protected HttpIntegrationTestFixture(string targetProjectParentDirectory)
        {
            TestDicomWebServer = TestDicomWebServerFactory.GetTestDicomWebServer(typeof(TStartup));

            RecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

            Client = TestDicomWebServer.GetDicomWebClient(RecyclableMemoryStreamManager);

            IsUsingInProcTestServer = TestDicomWebServer is InProcTestDicomWebServer;
        }

        public bool IsUsingInProcTestServer { get; }

        public HttpClient HttpClient => Client.HttpClient;

        protected TestDicomWebServer TestDicomWebServer { get; private set; }

        public RecyclableMemoryStreamManager RecyclableMemoryStreamManager { get; }

        public DicomWebClient Client { get; }

        public void Dispose()
        {
            HttpClient.Dispose();
            TestDicomWebServer?.Dispose();
        }
    }
}
