// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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

            Client = GetDicomWebClient();

            IsUsingInProcTestServer = TestDicomWebServer is InProcTestDicomWebServer;
        }

        public bool IsUsingInProcTestServer { get; }

        public HttpClient HttpClient => Client.HttpClient;

        protected TestDicomWebServer TestDicomWebServer { get; private set; }

        public RecyclableMemoryStreamManager RecyclableMemoryStreamManager { get; }

        public DicomWebClient Client { get; }

        public DicomWebClient GetDicomWebClient()
        {
            return GetDicomWebClient(TestApplications.GlobalAdminServicePrincipal);
        }

        public DicomWebClient GetDicomWebClient(TestApplication clientApplication)
        {
            var httpClient = new HttpClient(new SessionMessageHandler(TestDicomWebServer.CreateMessageHandler())) { BaseAddress = TestDicomWebServer.BaseAddress };

            (bool enabled, string tokenUrl) securitySettings = (AuthenticationSettings.SecurityEnabled, AuthenticationSettings.TokenUrl);

            return new DicomWebClient(httpClient, RecyclableMemoryStreamManager, clientApplication, securitySettings);
        }

        public void Dispose()
        {
            HttpClient.Dispose();
            TestDicomWebServer?.Dispose();
        }

        /// <summary>
        /// An <see cref="HttpMessageHandler"/> that maintains session consistency between requests.
        /// </summary>
        private class SessionMessageHandler : DelegatingHandler
        {
            private string _sessionToken;

            public SessionMessageHandler(HttpMessageHandler innerHandler)
                : base(innerHandler)
            {
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (!string.IsNullOrEmpty(_sessionToken))
                {
                    request.Headers.TryAddWithoutValidation("x-ms-session-token", _sessionToken);
                }

                request.Headers.TryAddWithoutValidation("x-ms-consistency-level", "Session");

                HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

                if (response.Headers.TryGetValues("x-ms-session-token", out IEnumerable<string> tokens))
                {
                    _sessionToken = tokens.SingleOrDefault();
                }

                return response;
            }
        }
    }
}
