// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Blob;
using Microsoft.Health.Dicom.Blob.Features.Export;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Blob.Utilities;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Registration;

namespace Microsoft.Extensions.DependencyInjection;

public static class DicomFunctionsBuilderRegistrationExtensions
{
    /// <summary>
    /// Adds the metadata store for the DICOM functions.
    /// </summary>
    /// <param name="functionsBuilder">The DICOM functions builder instance.</param>
    /// <param name="containerName">The name of the metadata container.</param>
    /// <param name="configuration">The configuration for the function.</param>
    /// <returns>The functions builder.</returns>
    public static IDicomFunctionsBuilder AddMetadataStorageDataStore(
        this IDicomFunctionsBuilder functionsBuilder,
        IConfiguration configuration,
        string containerName)
    {
        EnsureArg.IsNotNull(functionsBuilder, nameof(functionsBuilder));
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        var blobConfig = configuration.GetSection(BlobServiceClientOptions.DefaultSectionName);
        functionsBuilder.Services
            .AddSingleton<MetadataStoreConfigurationSection>()
            .AddTransient<IStoreConfigurationSection>(sp => sp.GetRequiredService<MetadataStoreConfigurationSection>())
            .AddPersistence<IMetadataStore, BlobMetadataStore, LoggingMetadataStore>()
            .AddBlobServiceClient(blobConfig)
            .Configure<BlobContainerConfiguration>(Constants.MetadataContainerConfigurationName, c => c.ContainerName = containerName);

        functionsBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IExportSinkProvider, AzureBlobExportSinkProvider>());

        return functionsBuilder;
    }
}
