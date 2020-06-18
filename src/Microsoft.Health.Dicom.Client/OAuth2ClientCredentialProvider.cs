// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.Health.Dicom.Client
{
    public class OAuth2ClientCredentialProvider : CredentialProvider
    {
        private readonly OAuth2ClientCredentialConfiguration _oAuth2ClientCredentialConfiguration;
        private readonly HttpClient _httpClient;

        public OAuth2ClientCredentialProvider(HttpClient httpClient, OAuth2ClientCredentialConfiguration oAuth2ClientCredentialConfiguration)
        {
            EnsureArg.IsNotNull(httpClient, nameof(httpClient));
            EnsureArg.IsNotNull(oAuth2ClientCredentialConfiguration, nameof(oAuth2ClientCredentialConfiguration));

            _httpClient = httpClient;
            _oAuth2ClientCredentialConfiguration = oAuth2ClientCredentialConfiguration;
        }

        protected override async Task<string> BearerTokenFunction(CancellationToken cancellationToken)
        {
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.ClientId, _oAuth2ClientCredentialConfiguration.ClientId),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.ClientSecret, _oAuth2ClientCredentialConfiguration.ClientSecret),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.GrantType, OpenIdConnectGrantTypes.ClientCredentials),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.Scope, _oAuth2ClientCredentialConfiguration.Scope),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.Resource, _oAuth2ClientCredentialConfiguration.Resource),
            };

            using var formContent = new FormUrlEncodedContent(formData);
            using HttpResponseMessage tokenResponse = await _httpClient.PostAsync(_oAuth2ClientCredentialConfiguration.TokenUri, formContent, cancellationToken);

            var openIdConnectMessage = new OpenIdConnectMessage(await tokenResponse.Content.ReadAsStringAsync());
            return openIdConnectMessage.AccessToken;
        }
    }
}
