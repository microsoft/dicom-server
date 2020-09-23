// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Health.Development.IdentityProvider;

namespace Microsoft.Health.Dicom.Web
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            IWebHost host = WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, builder) =>
                {
                    IConfigurationRoot builtConfig = builder.Build();

                    var keyVaultEndpoint = builtConfig["KeyVault:Endpoint"];
                    if (!string.IsNullOrEmpty(keyVaultEndpoint))
                    {
                        var azureServiceTokenProvider = new AzureServiceTokenProvider();
                        var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                        builder.AddAzureKeyVault(keyVaultEndpoint, keyVaultClient, new DefaultKeyVaultSecretManager());
                    }

                    builder.AddDevelopmentAuthEnvironmentIfConfigured(builtConfig, "DicomServer");
                })
                .ConfigureKestrel(option => option.Limits.MaxRequestBodySize = int.MaxValue) // When hosted on Kestrel, it's allowed to upload >2GB file, set to 2GB by default
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
