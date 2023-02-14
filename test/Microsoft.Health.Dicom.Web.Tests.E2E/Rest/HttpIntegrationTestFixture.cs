// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Client.Authentication;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Microsoft.IO;
using NSubstitute;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

public class HttpIntegrationTestFixture<TStartup> : IDisposable
{
    private readonly Dictionary<(string, string), AuthenticationHttpMessageHandler> _authenticationHandlers = new Dictionary<(string, string), AuthenticationHttpMessageHandler>();

    public HttpIntegrationTestFixture()
        : this(TestServerFeatureSettingType.None)
    { }

    protected HttpIntegrationTestFixture(TestServerFeatureSettingType featureSettingType) : this(new[] { featureSettingType })
    { }

    protected HttpIntegrationTestFixture(TestServerFeatureSettingType[] featureSettingTypes)
    {
        TestDicomWebServer = TestDicomWebServerFactory.GetTestDicomWebServer(typeof(TStartup), featureSettingTypes);
    }

    public bool IsInProcess => TestDicomWebServer is InProcTestDicomWebServer;

    public TestDicomWebServer TestDicomWebServer { get; }

    public RecyclableMemoryStreamManager RecyclableMemoryStreamManager { get; } = new RecyclableMemoryStreamManager();

    public IDicomWebClient GetDicomWebClient(string apiVersion = DicomApiVersions.V1)
    {
        return GetDicomWebClient(TestApplications.GlobalAdminServicePrincipal, apiVersion: apiVersion);
    }

    public IDicomWebClient GetDicomWebClient(TestApplication clientApplication, TestUser testUser = null, string apiVersion = DicomApiVersions.V1)
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
                    var credentialConfiguration = new OAuth2UserPasswordCredentialOptions(
                        AuthenticationSettings.TokenUri,
                        AuthenticationSettings.Resource,
                        AuthenticationSettings.Scope,
                        clientApplication.ClientId,
                        clientApplication.ClientSecret,
                        testUser.UserId,
                        testUser.Password);

                    IOptionsMonitor<OAuth2UserPasswordCredentialOptions> optionsMonitor = CreateOptionsMonitor(credentialConfiguration);
                    credentialProvider = new OAuth2UserPasswordCredentialProvider(optionsMonitor, new HttpClient(messageHandler) { BaseAddress = TestDicomWebServer.BaseAddress });
                }
                else
                {
                    var credentialConfiguration = new OAuth2ClientCredentialOptions(
                        AuthenticationSettings.TokenUri,
                        AuthenticationSettings.Resource,
                        AuthenticationSettings.Scope,
                        clientApplication.ClientId,
                        clientApplication.ClientSecret);

                    IOptionsMonitor<OAuth2ClientCredentialOptions> optionsMonitor = CreateOptionsMonitor(credentialConfiguration);
                    credentialProvider = new OAuth2ClientCredentialProvider(optionsMonitor, new HttpClient(messageHandler) { BaseAddress = TestDicomWebServer.BaseAddress });
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

        var dicomWebClient = new DicomWebClient(httpClient, apiVersion)
        {
            GetMemoryStream = () => RecyclableMemoryStreamManager.GetStream(),
        };
        return dicomWebClient;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            TestDicomWebServer.Dispose();
        }
    }

    private static IOptionsMonitor<T> CreateOptionsMonitor<T>(T configuration)
    {
        var optionsMonitor = Substitute.For<IOptionsMonitor<T>>();
        optionsMonitor.CurrentValue.Returns(configuration);
        optionsMonitor.Get(default).ReturnsForAnyArgs(configuration);

        return optionsMonitor;
    }
}
