// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Clients
{
    public class TestDicomWebClient : DicomWebClient
    {
        private readonly Dictionary<string, string> _bearerTokens = new Dictionary<string, string>();

        public TestDicomWebClient(HttpClient httpClient, RecyclableMemoryStreamManager recyclableMemoryStreamManager, TestApplication testApplication, Uri tokenUri)
            : base(httpClient, recyclableMemoryStreamManager, tokenUri)
        {
            SetupAuthenticationAsync(testApplication).GetAwaiter().GetResult();
        }

        private async Task SetupAuthenticationAsync(TestApplication clientApplication, TestUser user = null)
        {
            if (SecurityEnabled && clientApplication != TestApplications.InvalidClient)
            {
                var tokenKey = $"{clientApplication.ClientId}:{(user == null ? string.Empty : user.UserId)}";

                if (!_bearerTokens.TryGetValue(tokenKey, out string bearerToken))
                {
                    await this.AuthenticateOpenIdClientCredentials(
                        clientApplication.ClientId,
                        clientApplication.ClientSecret,
                        AuthenticationSettings.Resource,
                        AuthenticationSettings.Scope,
                        cancellationToken: default);

                    _bearerTokens[tokenKey] = HttpClient.DefaultRequestHeaders?.Authorization?.Parameter;

                    return;
                }

                SetBearerToken(bearerToken);
            }
        }
    }
}
