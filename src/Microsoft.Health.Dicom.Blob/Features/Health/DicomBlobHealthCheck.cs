// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Health;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Dicom.Blob.Utilities;
using Microsoft.Health.Encryption.Customer.Health;

namespace Microsoft.Health.Dicom.Blob.Features.Health;

/// <summary>
/// Checks for the DICOM blob service health.
/// </summary>
public class DicomBlobHealthCheck<TStoreConfigurationSection> : BlobHealthCheck
    where TStoreConfigurationSection : IStoreConfigurationSection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DicomBlobHealthCheck{TStoreConfigurationSection}"/> class.
    /// </summary>
    /// <param name="client">The blob client factory.</param>
    /// <param name="namedBlobContainerConfigurationAccessor">The IOptions accessor to get a named blob container version.</param>
    /// <param name="storeConfigurationSection"></param>
    /// <param name="testProvider">The test provider.</param>
    /// <param name="customerKeyHealthCache">The cached result of the customer key health status.</param>
    /// <param name="logger">The logger.</param>
    public DicomBlobHealthCheck(
        BlobServiceClient client,
        IOptionsSnapshot<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
        TStoreConfigurationSection storeConfigurationSection,
        IBlobClientTestProvider testProvider,
        ValueCache<CustomerKeyHealth> customerKeyHealthCache,
        ILogger<DicomBlobHealthCheck<TStoreConfigurationSection>> logger)
        : base(
              client,
              namedBlobContainerConfigurationAccessor,
              storeConfigurationSection.ContainerConfigurationName,
              testProvider,
              customerKeyHealthCache,
              logger)
    {
    }
}
