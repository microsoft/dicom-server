// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using EnsureThat;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Health.Dicom.Functions.Client.Extensions;

internal static class AzureComponentFactoryExtensions
{
    public static BlobServiceClient CreateBlobServiceClient(this AzureComponentFactory factory, IConfigurationSection configuration)
        => factory.CreateClient<BlobClientOptions, BlobServiceClient>(
            configuration,
            uriOptions => uriOptions.BlobServiceUri,
            (uri, credential, clientOptions) => new BlobServiceClient(uri, credential, clientOptions));

    public static QueueServiceClient CreateQueueServiceClient(this AzureComponentFactory factory, IConfigurationSection configuration)
        => factory.CreateClient<QueueClientOptions, QueueServiceClient>(
            configuration,
            uriOptions => uriOptions.QueueServiceUri,
            (uri, credential, clientOptions) => new QueueServiceClient(uri, credential, clientOptions));

    public static TableServiceClient CreateTableServiceClient(this AzureComponentFactory factory, IConfigurationSection configuration)
        => factory.CreateClient<TableClientOptions, TableServiceClient>(
            configuration,
            uriOptions => uriOptions.TableServiceUri,
            (uri, credential, clientOptions) => new TableServiceClient(uri, credential, clientOptions));

    private static TClient CreateClient<TOptions, TClient>(
        this AzureComponentFactory factory,
        IConfigurationSection configuration,
        Func<StorageServiceUriOptions, Uri> uriSelector,
        Func<Uri, TokenCredential, TOptions, TClient> clientFactory)
    {
        EnsureArg.IsNotNull(factory, nameof(factory));
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        TokenCredential credential = factory.CreateTokenCredential(configuration);
        TOptions options = (TOptions)factory.CreateClientOptions(typeof(TOptions), null, configuration);

        if (configuration.Value is null)
        {
            StorageServiceUriOptions serviceUriOptions = configuration.Get<StorageServiceUriOptions>();
            Uri serviceUri = serviceUriOptions is null ? null : uriSelector(serviceUriOptions);
            if (serviceUri != null)
                return clientFactory(serviceUri, credential, options);
        }

        return (TClient)factory.CreateClient(typeof(TClient), configuration, credential, options);
    }

    private sealed class StorageServiceUriOptions
    {
        private Uri _blobServiceUri;
        private Uri _queueServiceUri;
        private Uri _tableServiceUri;

        public Uri BlobServiceUri
        {
            get => _blobServiceUri ?? CreateStorageServiceUri("blob");
            set => _blobServiceUri = value;
        }

        public Uri QueueServiceUri
        {
            get => _queueServiceUri ?? CreateStorageServiceUri("queue");
            set => _queueServiceUri = value;
        }

        public Uri TableServiceUri
        {
            get => _tableServiceUri ?? CreateStorageServiceUri("table");
            set => _tableServiceUri = value;
        }

        public string AccountName { get; set; }

        private Uri CreateStorageServiceUri(string service)
            => string.IsNullOrEmpty(AccountName)
                ? null
                : new Uri(string.Format(CultureInfo.InvariantCulture, "https://{0}.{1}.core.windows.net", AccountName, service));
    }
}
