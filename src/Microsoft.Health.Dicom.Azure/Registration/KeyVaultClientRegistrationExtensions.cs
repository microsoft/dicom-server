// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure.Identity;
using EnsureThat;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Azure.Config;
using Microsoft.Health.Dicom.AzureKeyVault;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Registration;

namespace Microsoft.Health.Dicom.Azure.Registration;

public static class KeyVaultClientRegistrationExtensions
{
    public static IDicomServerBuilder AddKeyVaultClient(
           this IDicomServerBuilder dicomServerBuilder,
           IConfiguration configuration,
           Action<KeyVaultConfiguration> configureAction = null)
    {
        EnsureArg.IsNotNull(dicomServerBuilder, nameof(dicomServerBuilder));

        EnsureArg.IsNotNull(configuration, nameof(configuration));

        var config = new KeyVaultConfiguration();

        configuration.GetSection(KeyVaultConfiguration.SectionName).Bind(config);
        configureAction?.Invoke(config);

        // conditional KeyVault registration to support running on local docker
        if (!string.IsNullOrWhiteSpace(config.Endpoint))
        {
            dicomServerBuilder.Services.AddAzureClients(
                    builder =>
                    {
                        builder.AddSecretClient(new Uri(config.Endpoint))
                        .WithCredential(new DefaultAzureCredential());
                    });

            dicomServerBuilder.Services.AddScoped<ISecretStore, KeyVaultSecretStore>();
        }

        return dicomServerBuilder;
    }
}
