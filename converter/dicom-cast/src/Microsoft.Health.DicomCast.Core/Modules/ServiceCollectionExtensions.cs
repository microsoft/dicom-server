// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net.Http;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Client;
using Microsoft.Health.Extensions.DependencyInjection;
using IHttpClientFactory = Microsoft.IdentityModel.Clients.ActiveDirectory.IHttpClientFactory;

namespace Microsoft.Health.DicomCast.Core.Modules
{
    public static class ServiceCollectionExtensions
    {
        public static void AddNamedManagedIdentityCredentialProvider(this IServiceCollection serviceCollection, ManagedIdentityCredentialConfiguration managedIdentityCredentialConfiguration, string name)
        {
            EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
            EnsureArg.IsNotNull(managedIdentityCredentialConfiguration, nameof(managedIdentityCredentialConfiguration));
            EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));

            serviceCollection.Add(provider =>
                {
                    var options = Options.Create(managedIdentityCredentialConfiguration);
                    var httpClientFactory = provider.GetService<IHttpClientFactory>();
                    var credentialProvider = new ManagedIdentityCredentialProvider(options, httpClientFactory);
                    return new NamedCredentialProvider(name, credentialProvider);
                })
                .Singleton()
                .AsService<NamedCredentialProvider>();
        }

        public static void AddNamedOAuth2ClientCredentialProvider(this IServiceCollection serviceCollection, OAuth2ClientCredentialConfiguration oAuth2ClientCredentialConfiguration, string name)
        {
            EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
            EnsureArg.IsNotNull(oAuth2ClientCredentialConfiguration, nameof(oAuth2ClientCredentialConfiguration));
            EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));

            serviceCollection.Add(provider =>
                {
                    var options = Options.Create(oAuth2ClientCredentialConfiguration);
                    var httpClient = new HttpClient();
                    var credentialProvider = new OAuth2ClientCredentialProvider(options, httpClient);
                    return new NamedCredentialProvider(name, credentialProvider);
                })
                .Singleton()
                .AsService<NamedCredentialProvider>();
        }

        public static void AddNamedOAuth2UserPasswordCredentialProvider(this IServiceCollection serviceCollection, OAuth2UserPasswordCredentialConfiguration oAuth2UserPasswordCredentialConfiguration, string name)
        {
            EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
            EnsureArg.IsNotNull(oAuth2UserPasswordCredentialConfiguration, nameof(oAuth2UserPasswordCredentialConfiguration));
            EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));

            serviceCollection.Add(provider =>
                {
                    var options = Options.Create(oAuth2UserPasswordCredentialConfiguration);
                    var httpClient = new HttpClient();
                    var credentialProvider = new OAuth2UserPasswordCredentialProvider(options, httpClient);
                    return new NamedCredentialProvider(name, credentialProvider);
                })
                .Singleton()
                .AsService<NamedCredentialProvider>();
        }
    }
}
