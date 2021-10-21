// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Client;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class HttpIntegrationTestFixture<TStartup> : IDisposable
    {
        private readonly Dictionary<(string, string), AuthenticationHttpMessageHandler> _authenticationHandlers = new Dictionary<(string, string), AuthenticationHttpMessageHandler>();

        public HttpIntegrationTestFixture()
            : this(Path.Combine("src"))
        {
        }

        protected HttpIntegrationTestFixture(string targetProjectParentDirectory, bool enableDataPartitions = false)
        {
            TestDicomWebServer = TestDicomWebServerFactory.GetTestDicomWebServer(typeof(TStartup), enableDataPartitions);

            RecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

            Client = GetDicomWebClient();

            IsUsingInProcTestServer = TestDicomWebServer is InProcTestDicomWebServer;
        }

        public bool IsUsingInProcTestServer { get; }

        public HttpClient HttpClient => Client.HttpClient;

        protected TestDicomWebServer TestDicomWebServer { get; private set; }

        public RecyclableMemoryStreamManager RecyclableMemoryStreamManager { get; }

        public IDicomWebClient Client { get; }

        public IDicomWebClient GetDicomWebClient()
        {
            return GetDicomWebClient(TestApplications.GlobalAdminServicePrincipal);
        }

        public IDicomWebClient GetDicomWebClient(TestApplication clientApplication, TestUser testUser = null)
        {
            EnsureArg.IsNotNull(clientApplication, nameof(clientApplication));
            HttpMessageHandler messageHandler = TestDicomWebServer.CreateMessageHandler();
            if (AuthenticationSettings.SecurityEnabled && !clientApplication.Equals(TestApplications.InvalidClient))
            {
                if (_authenticationHandlers.ContainsKey((clientApplication.ClientId, testUser?.UserId)))
                {
                    messageHandler = _authenticationHandlers[(clientApplication.ClientId, testUser?.UserId)];
                }
                else
                {
                    ICredentialProvider credentialProvider;
                    if (testUser != null)
                    {
                        var credentialConfiguration = new OAuth2UserPasswordCredentialConfiguration(
                            AuthenticationSettings.TokenUri,
                            AuthenticationSettings.Resource,
                            AuthenticationSettings.Scope,
                            clientApplication.ClientId,
                            clientApplication.ClientSecret,
                            testUser.UserId,
                            testUser.Password);

                        credentialProvider = new OAuth2UserPasswordCredentialProvider(Options.Create(credentialConfiguration), new HttpClient(messageHandler) { BaseAddress = TestDicomWebServer.BaseAddress });
                    }
                    else
                    {
                        var credentialConfiguration = new OAuth2ClientCredentialConfiguration(
                            AuthenticationSettings.TokenUri,
                            AuthenticationSettings.Resource,
                            AuthenticationSettings.Scope,
                            clientApplication.ClientId,
                            clientApplication.ClientSecret);

                        credentialProvider = new OAuth2ClientCredentialProvider(Options.Create(credentialConfiguration), new HttpClient(messageHandler) { BaseAddress = TestDicomWebServer.BaseAddress });
                    }

                    var authHandler = new AuthenticationHttpMessageHandler(credentialProvider)
                    {
                        InnerHandler = messageHandler,
                    };

                    _authenticationHandlers.Add((clientApplication.ClientId, testUser?.UserId), authHandler);
                    messageHandler = authHandler;
                }
            }

            var httpClient = new HttpClient(messageHandler) { BaseAddress = TestDicomWebServer.BaseAddress };

            var dicomWebClient = new DicomWebClient(httpClient, DicomApiVersions.V1Prerelease)
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
