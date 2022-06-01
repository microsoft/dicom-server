using System.Net.Http;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Client;
using Microsoft.Health.Extensions.DependencyInjection;
using IHttpClientFactory = Microsoft.IdentityModel.Clients.ActiveDirectory.IHttpClientFactory;

namespace DicomUploaderFunction.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddNamedManagedIdentityCredentialProvider(this IServiceCollection serviceCollection, IConfiguration managedIdentityCredentialConfiguration, string name)
    {
        EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
        EnsureArg.IsNotNull(managedIdentityCredentialConfiguration, nameof(managedIdentityCredentialConfiguration));
        EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));

        serviceCollection.Configure<ManagedIdentityCredentialConfiguration>(name, managedIdentityCredentialConfiguration);

        serviceCollection.Add(provider =>
            {
                IOptionsMonitor<ManagedIdentityCredentialConfiguration> options = provider.GetService<IOptionsMonitor<ManagedIdentityCredentialConfiguration>>();
                var httpClientFactory = provider.GetService<IHttpClientFactory>();
                var credentialProvider = new ManagedIdentityCredentialProvider(options, httpClientFactory, name);
                return new NamedCredentialProvider(name, credentialProvider);
            })
            .Singleton()
            .AsSelf();
    }

    public static void AddNamedOAuth2ClientCertificateCredentialProvider(this IServiceCollection serviceCollection, IConfiguration oAuth2ClientCertificateCredentialConfiguration, string name)
    {
        EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
        EnsureArg.IsNotNull(oAuth2ClientCertificateCredentialConfiguration, nameof(oAuth2ClientCertificateCredentialConfiguration));
        EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));

        serviceCollection.Configure<OAuth2ClientCertificateCredentialConfiguration>(name, oAuth2ClientCertificateCredentialConfiguration);

        serviceCollection.Add(provider =>
            {
                IOptionsMonitor<OAuth2ClientCertificateCredentialConfiguration> options = provider.GetService<IOptionsMonitor<OAuth2ClientCertificateCredentialConfiguration>>();
                var httpClient = new HttpClient();
                var credentialProvider = new OAuth2ClientCertificateCredentialProvider(options, httpClient, name);
                return new NamedCredentialProvider(name, credentialProvider);
            })
            .Singleton()
            .AsSelf();
    }

    public static void AddNamedOAuth2ClientCredentialProvider(this IServiceCollection serviceCollection, IConfiguration oAuth2ClientCredentialConfiguration, string name)
    {
        EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
        EnsureArg.IsNotNull(oAuth2ClientCredentialConfiguration, nameof(oAuth2ClientCredentialConfiguration));
        EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));

        serviceCollection.Configure<OAuth2ClientCredentialConfiguration>(name, oAuth2ClientCredentialConfiguration);
        serviceCollection.Add(provider =>
            {
                IOptionsMonitor<OAuth2ClientCredentialConfiguration> options = provider.GetService<IOptionsMonitor<OAuth2ClientCredentialConfiguration>>();
                var httpClient = new HttpClient();
                var credentialProvider = new OAuth2ClientCredentialProvider(options, httpClient, name);
                return new NamedCredentialProvider(name, credentialProvider);
            })
            .Singleton()
            .AsSelf();
    }

    public static void AddNamedOAuth2UserPasswordCredentialProvider(this IServiceCollection serviceCollection, IConfiguration oAuth2UserPasswordCredentialConfiguration, string name)
    {
        EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
        EnsureArg.IsNotNull(oAuth2UserPasswordCredentialConfiguration, nameof(oAuth2UserPasswordCredentialConfiguration));
        EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));

        serviceCollection.Configure<OAuth2UserPasswordCredentialConfiguration>(name, oAuth2UserPasswordCredentialConfiguration);
        serviceCollection.Add(provider =>
            {
                IOptionsMonitor<OAuth2UserPasswordCredentialConfiguration> options = provider.GetService<IOptionsMonitor<OAuth2UserPasswordCredentialConfiguration>>();
                var httpClient = new HttpClient();
                var credentialProvider = new OAuth2UserPasswordCredentialProvider(options, httpClient, name);
                return new NamedCredentialProvider(name, credentialProvider);
            })
            .Singleton()
            .AsSelf();
    }
}