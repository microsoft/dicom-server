// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Client;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Modules;

namespace Microsoft.Health.DicomCast.Core.Extensions;

public static class IHttpClientBuilderRegistrationExtensions
{
    public static void AddAuthenticationHandler(this IHttpClientBuilder httpClientBuilder, IServiceCollection services, IConfigurationSection authenticationConfigurationSection, string credentialProviderName)
    {
        EnsureArg.IsNotNull(httpClientBuilder, nameof(httpClientBuilder));
        EnsureArg.IsNotNull(services, nameof(services));
        EnsureArg.IsNotNull(authenticationConfigurationSection, nameof(authenticationConfigurationSection));
        EnsureArg.IsNotNullOrWhiteSpace(credentialProviderName, nameof(credentialProviderName));

        var auth = new AuthenticationConfiguration();
        authenticationConfigurationSection.Bind(auth);
        if (!auth.Enabled)
        {
            return;
        }

        switch (auth.AuthenticationType)
        {
            case AuthenticationType.ManagedIdentity:
                services.AddNamedManagedIdentityCredentialProvider(authenticationConfigurationSection.GetSection(nameof(auth.ManagedIdentityCredential)), credentialProviderName);
                break;
            case AuthenticationType.OAuth2ClientCertificateCredential:
                services.AddNamedOAuth2ClientCertificateCredentialProvider(authenticationConfigurationSection.GetSection(nameof(auth.OAuth2ClientCertificateCredential)), credentialProviderName);
                break;
            case AuthenticationType.OAuth2ClientCredential:
                services.AddNamedOAuth2ClientCredentialProvider(authenticationConfigurationSection.GetSection(nameof(auth.OAuth2ClientCredential)), credentialProviderName);
                break;
            case AuthenticationType.OAuth2UserPasswordCredential:
                services.AddNamedOAuth2UserPasswordCredentialProvider(authenticationConfigurationSection.GetSection(nameof(auth.OAuth2UserPasswordCredential)), credentialProviderName);
                break;
        }

        httpClientBuilder
            .AddHttpMessageHandler(x => new AuthenticationHttpMessageHandler(x.ResolveNamedCredentialProvider(credentialProviderName)));
    }
}
