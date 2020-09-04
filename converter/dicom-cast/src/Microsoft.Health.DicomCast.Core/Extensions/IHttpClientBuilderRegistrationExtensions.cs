// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Client;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Modules;

namespace Microsoft.Health.DicomCast.Core.Extensions
{
    public static class IHttpClientBuilderRegistrationExtensions
    {
        public static void AddAuthenticationHandler(this IHttpClientBuilder httpClientBuilder, IServiceCollection services, AuthenticationConfiguration authenticationConfiguration, string credentialProviderName)
        {
            EnsureArg.IsNotNull(httpClientBuilder, nameof(httpClientBuilder));
            EnsureArg.IsNotNull(services, nameof(services));
            EnsureArg.IsNotNull(authenticationConfiguration, nameof(authenticationConfiguration));
            EnsureArg.IsNotNullOrWhiteSpace(credentialProviderName, nameof(credentialProviderName));

            if (!authenticationConfiguration.Enabled)
            {
                return;
            }

            switch (authenticationConfiguration.AuthenticationType)
            {
                case AuthenticationType.ManagedIdentity:
                    services.AddNamedManagedIdentityCredentialProvider(authenticationConfiguration.ManagedIdentityCredential, credentialProviderName);
                    break;
                case AuthenticationType.OAuth2ClientCredential:
                    services.AddNamedOAuth2ClientCredentialProvider(authenticationConfiguration.OAuth2ClientCredential, credentialProviderName);
                    break;
                case AuthenticationType.OAuth2UserPasswordCredential:
                    services.AddNamedOAuth2UserPasswordCredentialProvider(authenticationConfiguration.OAuth2UserPasswordCredential, credentialProviderName);
                    break;
            }

            httpClientBuilder
                .AddHttpMessageHandler(x => new AuthenticationHttpMessageHandler(x.ResolveNamedCredentialProvider(credentialProviderName)));
        }
    }
}
