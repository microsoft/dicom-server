// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Metadata;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IBlobServiceBuilder AddAzureBlobServiceClient(
            this IServiceCollection services,
            IConfiguration configurationRoot)
        {
            EnsureArg.IsNotNull(services, nameof(services));
            EnsureArg.IsNotNull(configurationRoot, nameof(configurationRoot));

            services.Configure<BlobContainerConfiguration>(
                Constants.ContainerConfigurationName,
                containerConfiguration => configurationRoot
                    .GetSection("DicomWeb:MetadataStore")
                    .Bind(containerConfiguration));

            // TODO: Leverage Shared Components
            var config = new BlobDataStoreConfiguration();
            configurationRoot.GetSection("BlobStore").Bind(config);

            if (string.IsNullOrEmpty(config.ConnectionString) && config.AuthenticationType == BlobDataStoreAuthenticationType.ConnectionString)
            {
                config.ConnectionString = "UseDevelopmentStorage=true";
            }

            services.AddSingleton(config);

            // Configure the blob client default request options and retry logic
            var blobClientOptions = new BlobClientOptions();
            blobClientOptions.Retry.MaxRetries = config.RequestOptions.ExponentialRetryMaxAttempts;
            blobClientOptions.Retry.Mode = RetryMode.Exponential;
            blobClientOptions.Retry.Delay = TimeSpan.FromSeconds(config.RequestOptions.ExponentialRetryBackoffDeltaInSeconds);
            blobClientOptions.Retry.NetworkTimeout = TimeSpan.FromMinutes(config.RequestOptions.ServerTimeoutInMinutes);

            BlobServiceClient client = config.AuthenticationType == BlobDataStoreAuthenticationType.ManagedIdentity
                ? new BlobServiceClient(new Uri(config.ConnectionString), new DefaultAzureCredential(), blobClientOptions)
                : new BlobServiceClient(config.ConnectionString, blobClientOptions);

            services.AddSingleton(client);

            return new BlobServiceBuilder(services, config);
        }
    }
}
