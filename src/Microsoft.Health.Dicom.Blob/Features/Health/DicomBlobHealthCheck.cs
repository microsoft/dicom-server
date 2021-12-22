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
using Microsoft.Health.Dicom.Blob.Utilities;

namespace Microsoft.Health.Dicom.Blob.Features.Health
{
    /// <summary>
    /// Checks for the DICOM blob service health.
    /// </summary>
    public class DicomBlobHealthCheck<TStoreConfigurationAware> : BlobHealthCheck where TStoreConfigurationAware : IStoreConfigurationAware
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DicomBlobHealthCheck{TBlobContainerDetailAware}"/> class.
        /// </summary>
        /// <param name="client">The blob client factory.</param>
        /// <param name="namedBlobContainerConfigurationAccessor">The IOptions accessor to get a named blob container version.</param>
        /// <param name="blobContainerDetailAware"></param>
        /// <param name="testProvider">The test provider.</param>
        /// <param name="logger">The logger.</param>
        public DicomBlobHealthCheck(
            BlobServiceClient client,
            IOptionsSnapshot<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
            TStoreConfigurationAware blobContainerDetailAware,
            IBlobClientTestProvider testProvider,
            ILogger<DicomBlobHealthCheck<TStoreConfigurationAware>> logger)
            : base(
                  client,
                  namedBlobContainerConfigurationAccessor,
                  blobContainerDetailAware.Name,
                  testProvider,
                  logger)
        {
        }
    }
}
