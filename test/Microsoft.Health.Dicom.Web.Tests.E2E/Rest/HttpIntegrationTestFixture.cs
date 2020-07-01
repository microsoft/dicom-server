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
using Microsoft.Extensions.Options;
using Microsoft.Health.Client;
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
            HttpMessageHandler messageHandler = TestDicomWebServer.CreateMessageHandler();
            if (AuthenticationSettings.SecurityEnabled && !clientApplication.Equals(TestApplications.InvalidClient))
            {
                if (_authenticationHandlers.ContainsKey(clientApplication.ClientId))
                {
                    messageHandler = _authenticationHandlers[clientApplication.ClientId];
                }
                else
                {
                    var credentialConfiguration = new OAuth2ClientCredentialConfiguration(
                        AuthenticationSettings.TokenUri,
                        AuthenticationSettings.Resource,
                        AuthenticationSettings.Scope,
                        clientApplication.ClientId,
                        clientApplication.ClientSecret);
                    var credentialProvider = new OAuth2ClientCredentialProvider(Options.Create(credentialConfiguration), new HttpClient(messageHandler));
                    var authHandler = new AuthenticationHttpMessageHandler(credentialProvider)
                    {
                        InnerHandler = messageHandler,
                    };

                    _authenticationHandlers.Add(clientApplication.ClientId, authHandler);
                    messageHandler = authHandler;
                }
            }

            var httpClient = new HttpClient(messageHandler) { BaseAddress = TestDicomWebServer.BaseAddress };

            var dicomWebClient = new DicomWebClient(httpClient)
            {
                GetMemoryStream = () => RecyclableMemoryStreamManager.GetStream(),
            };
            return dicomWebClient;
        }

        public void Dispose()
        {
            HttpClient.Dispose();
            TestDicomWebServer?.Dispose();
        }
    }
}
