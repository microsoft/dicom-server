// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.Health.Dicom.Client
{
    public static class DicomWebClientAuthExtensions
    {
        /// <summary>
        /// Sets the authenticated token on the <see cref="IDicomWebClient"/> to the supplied resource via Managed Identity.
        /// </summary>
        /// <param name="dicomWebClient">The <see cref="IDicomWebClient"/> to authenticate.</param>
        /// <param name="resource">The resource to obtain a token to.</param>
        /// <param name="tenantId">The optional tenantId to use when requesting a token.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the request.</param>
        /// <returns>A <see cref="Task"/> representing the successful setting of the token.</returns>
        public static async Task AuthenticateWithManagedIdentity(this IDicomWebClient dicomWebClient, string resource, string tenantId = null, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));
            EnsureArg.IsNotNullOrWhiteSpace(resource, nameof(resource));

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync(resource, tenantId, cancellationToken);

            dicomWebClient.SetBearerToken(accessToken);
        }

        /// <summary>
        /// Sets the authenticated token on the <see cref="IDicomWebClient"/> via OpenId client credentials.
        /// </summary>
        /// <param name="dicomWebClient">The <see cref="IDicomWebClient"/> to authenticate.</param>
        /// <param name="clientId">The clientId of the application.</param>
        /// <param name="clientSecret">The clientSecret of the application.</param>
        /// <param name="resource">The resource to authenticate with.</param>
        /// <param name="scope">The scope to authenticate with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the request.</param>
        /// <returns>A <see cref="Task"/> representing the successful setting of the token.</returns>
        public static async Task AuthenticateOpenIdClientCredentials(
            this IDicomWebClient dicomWebClient,
            string clientId,
            string clientSecret,
            string resource,
            string scope,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));
            EnsureArg.IsNotNullOrWhiteSpace(clientId, nameof(clientId));
            EnsureArg.IsNotNullOrWhiteSpace(clientSecret, nameof(clientSecret));
            EnsureArg.IsNotNullOrWhiteSpace(resource, nameof(resource));
            EnsureArg.IsNotNullOrWhiteSpace(scope, nameof(scope));

            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.ClientId, clientId),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.ClientSecret, clientSecret),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.GrantType, OpenIdConnectGrantTypes.ClientCredentials),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.Scope, scope),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.Resource, resource),
            };

            await ObtainTokenAndSetOnClient(dicomWebClient, formData, cancellationToken);
        }

        /// <summary>
        /// Sets the authenticated token on the <see cref="IDicomWebClient"/> via OpenId user password.
        /// </summary>
        /// <param name="dicomWebClient">The <see cref="IDicomWebClient"/> to authenticate.</param>
        /// <param name="clientId">The clientId of the application.</param>
        /// <param name="clientSecret">The clientSecret of the application.</param>
        /// <param name="resource">The resource to authenticate with.</param>
        /// <param name="scope">The scope to authenticate with.</param>
        /// <param name="username">The username to authenticate.</param>
        /// <param name="password">The password to authenticate.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the request.</param>
        /// <returns>A <see cref="Task"/> representing the successful setting of the token.</returns>
        public static async Task AuthenticateOpenIdUserPassword(
            this IDicomWebClient dicomWebClient,
            string clientId,
            string clientSecret,
            string resource,
            string scope,
            string username,
            string password,
            CancellationToken cancellationToken)
        {
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.ClientId, clientId),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.ClientSecret, clientSecret),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.GrantType, OpenIdConnectGrantTypes.Password),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.Scope, scope),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.Resource, resource),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.Username, username),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.Password, password),
            };

            await ObtainTokenAndSetOnClient(dicomWebClient, formData, cancellationToken);
        }

        private static async Task ObtainTokenAndSetOnClient(IDicomWebClient dicomWebClient, List<KeyValuePair<string, string>> formData, CancellationToken cancellationToken)
        {
            using var formContent = new FormUrlEncodedContent(formData);
            using HttpResponseMessage tokenResponse = await dicomWebClient.HttpClient.PostAsync(dicomWebClient.TokenUri, formContent, cancellationToken);

            var openIdConnectMessage = new OpenIdConnectMessage(await tokenResponse.Content.ReadAsStringAsync());
            dicomWebClient.SetBearerToken(openIdConnectMessage.AccessToken);
        }
    }
}
