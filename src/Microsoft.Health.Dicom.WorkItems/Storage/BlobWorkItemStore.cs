// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Workitems;
using Microsoft.IO;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Metadata.Features.Storage
{
    /// <summary>
    /// Provides functionality for managing the DICOM instance work-item.
    /// </summary>
    public class BlobWorkitemStore : IWorkitemStore
    {
        private readonly BlobContainerClient _container;
        private readonly JsonSerializer _jsonSerializer;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public BlobWorkitemStore(
            BlobServiceClient client,
            JsonSerializer jsonSerializer,
            IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
            RecyclableMemoryStreamManager recyclableMemoryStreamManager)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(jsonSerializer, nameof(jsonSerializer));
            EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));

            var containerConfiguration = namedBlobContainerConfigurationAccessor.Get(Constants.ContainerConfigurationName);

            _container = client.GetBlobContainerClient(containerConfiguration.ContainerName);
            _jsonSerializer = jsonSerializer;
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="partitionKey"></param>
        /// <param name="workitemDataset"></param>
        /// <param name="queryTags"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<long> AddWorkitemAsync(int partitionKey, WorkitemDataset workitemDataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken)
        {
            return await Task.FromResult(0);
        }
    }
}
