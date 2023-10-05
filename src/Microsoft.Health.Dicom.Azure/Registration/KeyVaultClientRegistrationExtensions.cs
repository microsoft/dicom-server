// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure.Core.Extensions;
using Azure.Security.KeyVault.Secrets;
using EnsureThat;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Core.Extensions;
using Microsoft.Health.Dicom.Azure;
using Microsoft.Health.Dicom.Azure.KeyVault;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Registration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A collection of methods for registering Azure Key Vault clients.
/// </summary>
public static class KeyVaultClientRegistrationExtensions
{
    /// <summary>
    /// Adds a secret client for Azure Key Vault.
    /// </summary>
    /// <param name="builder">The DICOM server builder instance.</param>
    /// <param name="configuration">The configuration for the client.</param>
    /// <param name="configureOptions">Optional action for configuring the options.</param>
    /// <returns>The server builder.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configuration"/> is <see langword="null"/>.
    /// </exception>
    public static IDicomServerBuilder AddKeyVaultClient(
           this IDicomServerBuilder builder,
           IConfiguration configuration,
           Action<SecretClientOptions> configureOptions = null)
    {
        EnsureArg.IsNotNull(builder, nameof(builder));
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        builder.Services.AddKeyVaultClient(configuration, configureOptions);
        return builder;
    }

    /// <summary>
    /// Adds a secret client for Azure Key Vault.
    /// </summary>
    /// <param name="builder">The DICOM functions builder instance.</param>
    /// <param name="configuration">The configuration for the client.</param>
    /// <param name="configureOptions">Optional action for configuring the options.</param>
    /// <returns>The server builder.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configuration"/> is <see langword="null"/>.
    /// </exception>
    public static IDicomFunctionsBuilder AddKeyVaultClient(
           this IDicomFunctionsBuilder builder,
           IConfiguration configuration,
           Action<SecretClientOptions> configureOptions = null)
    {
        EnsureArg.IsNotNull(builder, nameof(builder));
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        builder.Services.AddKeyVaultClient(configuration, configureOptions);
        return builder;
    }

    private static IServiceCollection AddKeyVaultClient(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<SecretClientOptions> configureOptions)
    {
        EnsureArg.IsNotNull(services, nameof(services));
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        IConfigurationSection section = configuration.GetSection(KeyVaultSecretClientOptions.SectionName);

        var options = new KeyVaultSecretClientOptions();
        section.Bind(options);
        configureOptions?.Invoke(options);

        // Note: We can disable key vault in local development scenarios, like F5 or Docker
        if (options.VaultUri != null)
        {
            // Backfill from obsolete setting
#pragma warning disable CS0618
            if (options.Endpoint != null)
                section[nameof(KeyVaultSecretClientOptions.VaultUri)] = section[nameof(KeyVaultSecretClientOptions.Endpoint)];
#pragma warning restore CS0618

            services.AddAzureClients(builder =>
            {
                IAzureClientBuilder<SecretClient, SecretClientOptions> clientBuilder = builder
                    .AddSecretClient(section)
                    .WithRetryableCredential(section);

                if (configureOptions != null)
                    clientBuilder.ConfigureOptions(configureOptions);
            });

            services.AddScoped<ISecretStore, KeyVaultSecretStore>();
        }

        return services;
    }
}
