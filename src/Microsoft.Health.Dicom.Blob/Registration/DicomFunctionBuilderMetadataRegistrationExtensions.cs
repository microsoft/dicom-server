// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Blob;
using Microsoft.Health.Dicom.Blob.Features.Export;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Blob.Features.Telemetry;
using Microsoft.Health.Dicom.Blob.Utilities;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Registration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A collection of <see langword="static"/> extension methods for configuring blob services for Azure Functions.
/// </summary>
public static class DicomFunctionsBuilderRegistrationExtensions
{
    /// <summary>
    /// Adds the metadata store for the DICOM functions.
    /// </summary>
    /// <param name="functionsBuilder">The DICOM functions builder instance.</param>
    /// <param name="configuration">The host configuration for the functions.</param>
    /// <param name="functionSectionName">The name of the configuration section containing the functions.</param>
    /// <returns>The functions builder.</returns>
    public static IDicomFunctionsBuilder AddBlobStorage(
        this IDicomFunctionsBuilder functionsBuilder,
        IConfiguration configuration,
        string functionSectionName)
    {
        EnsureArg.IsNotNull(functionsBuilder, nameof(functionsBuilder));
        EnsureArg.IsNotNull(configuration, nameof(configuration));
        EnsureArg.IsNotNullOrWhiteSpace(functionSectionName, nameof(functionSectionName));

        // Common services
        IConfigurationSection blobConfig = configuration.GetSection(BlobServiceClientOptions.DefaultSectionName);
        functionsBuilder.Services
            .AddBlobServiceClient(blobConfig)
            .AddOptions<DicomBlobContainerOptions>()
            .Bind(blobConfig.GetSection(DicomBlobContainerOptions.SectionName))
            .ValidateDataAnnotations();

        // Metadata
        functionsBuilder.Services
            .AddSingleton<MetadataStoreConfigurationSection>()
            .AddTransient<IStoreConfigurationSection>(sp => sp.GetRequiredService<MetadataStoreConfigurationSection>())
            .AddPersistence<IMetadataStore, BlobMetadataStore>()
            .AddScoped<DicomFileNameWithPrefix>()
            .AddOptions<BlobContainerConfiguration>(Constants.MetadataContainerConfigurationName)
            .Configure<IOptionsMonitor<DicomBlobContainerOptions>>((c, o) => c.ContainerName = o.CurrentValue.Metadata);

        // Blob Files
        functionsBuilder.Services
            .AddSingleton<BlobStoreConfigurationSection>()
            .AddTransient<IStoreConfigurationSection>(sp => sp.GetRequiredService<BlobStoreConfigurationSection>())
            .AddPersistence<IFileStore, BlobFileStore>()
            .AddOptions<BlobContainerConfiguration>(Constants.BlobContainerConfigurationName)
            .Configure<IOptionsMonitor<DicomBlobContainerOptions>>((c, o) => c.ContainerName = o.CurrentValue.File);

        // Export
        functionsBuilder.Services
            .AddAzureBlobExportSink(
                o => configuration.GetSection(functionSectionName).GetSection(AzureBlobExportSinkProviderOptions.DefaultSection).Bind(o),
                o => blobConfig.Bind(o)); // Re-use the blob store's configuration

        // Telemetry
        functionsBuilder.Services
            .AddSingleton<BlobStoreMeter>()
            .AddSingleton<BlobRetrieveMeter>();

        return functionsBuilder;
    }
}
