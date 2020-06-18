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
    public class OAuth2UserCredentialProvider : CredentialProvider
    {
        private readonly OAuth2UserCredentialConfiguration _oAuth2UserCredentialConfiguration;
        private readonly HttpClient _httpClient;

        public OAuth2UserCredentialProvider(HttpClient httpClient, OAuth2UserCredentialConfiguration oAuth2UserCredentialConfiguration)
        {
            EnsureArg.IsNotNull(httpClient, nameof(httpClient));
            EnsureArg.IsNotNull(oAuth2UserCredentialConfiguration, nameof(oAuth2UserCredentialConfiguration));

            _httpClient = httpClient;
            _oAuth2UserCredentialConfiguration = oAuth2UserCredentialConfiguration;
        }

        protected override async Task<string> BearerTokenFunction(CancellationToken cancellationToken)
        {
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.ClientId, _oAuth2UserCredentialConfiguration.ClientId),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.ClientSecret, _oAuth2UserCredentialConfiguration.ClientSecret),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.GrantType, OpenIdConnectGrantTypes.ClientCredentials),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.Scope, _oAuth2UserCredentialConfiguration.Scope),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.Resource, _oAuth2UserCredentialConfiguration.Resource),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.Username, _oAuth2UserCredentialConfiguration.Username),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.Password, _oAuth2UserCredentialConfiguration.Password),
            };

            using var formContent = new FormUrlEncodedContent(formData);
            using HttpResponseMessage tokenResponse = await _httpClient.PostAsync(_oAuth2UserCredentialConfiguration.TokenUri, formContent, cancellationToken);

            var openIdConnectMessage = new OpenIdConnectMessage(await tokenResponse.Content.ReadAsStringAsync());
            return openIdConnectMessage.AccessToken;
        }
    }
}
