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
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class HttpIntegrationTestFixture<TStartup> : IDisposable
    {
        private Dictionary<string, AuthenticationHttpMessageHandler> _authenticationHandlers = new Dictionary<string, AuthenticationHttpMessageHandler>();

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
            var messageHandler = new SessionMessageHandler(TestDicomWebServer.CreateMessageHandler());
            if (AuthenticationSettings.SecurityEnabled)
            {
                if (_authenticationHandlers.ContainsKey(clientApplication.ClientId))
                {
                    messageHandler.InnerHandler = _authenticationHandlers[clientApplication.ClientId];
                }
                else
                {
                    var credentialConfiguration = new OAuth2ClientCredentialConfiguration(
                        AuthenticationSettings.TokenUri,
                        AuthenticationSettings.Resource,
                        AuthenticationSettings.Scope,
                        clientApplication.ClientId,
                        clientApplication.ClientSecret);
                    var credentialProvider = new OAuth2ClientCredentialProvider(new HttpClient(), credentialConfiguration);
                    var authHandler = new AuthenticationHttpMessageHandler(credentialProvider);
                    _authenticationHandlers.Add(clientApplication.ClientId, authHandler);
                    messageHandler.InnerHandler = authHandler;
                }
            }

            var httpClient = new HttpClient(messageHandler) { BaseAddress = TestDicomWebServer.BaseAddress };

            return new DicomWebClient(httpClient);
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
