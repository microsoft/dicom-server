// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to orchestrate the storing of the DICOM instance entry.
    /// </summary>
    public class WorkitemOrchestrator : IWorkitemOrchestrator
    {
        private readonly IDicomRequestContextAccessor _contextAccessor;
        private readonly IMetadataStore _metadataStore;
        private readonly IWorkitemStore _workitemStore;

        public WorkitemOrchestrator(
            IDicomRequestContextAccessor contextAccessor,
            IMetadataStore metadataStore,
            IWorkitemStore workitemStore,
            IDeleteService deleteService,
            IQueryTagService queryTagService)
        {
            _contextAccessor = EnsureArg.IsNotNull(contextAccessor, nameof(contextAccessor));
            _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
            _workitemStore = EnsureArg.IsNotNull(workitemStore, nameof(workitemStore));
        }

        /// <inheritdoc />
        public async Task StoreWorkitemEntryAsync(
            WorkitemDataset workitemDataset,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(workitemDataset, nameof(workitemDataset));
            var partitionKey = _contextAccessor.RequestContext.GetPartitionKey();

            // TODO: set instance uid - either from route param or from dataset

            // TODO: generate QueryTags list for workitem
            var queryTags = new List<QueryTag>();

            long watermark = await _workitemStore.AddWorkitemAsync(partitionKey, workitemDataset, queryTags, cancellationToken);

            // We have successfully created the index, store the file.
            await StoreWorkitemBlobAsync(workitemDataset, watermark, cancellationToken);

            // TODO: implement cleanup to delete the index if blob storage fails
        }


        private Task StoreWorkitemBlobAsync(
            WorkitemDataset dicomDataset,
            long version,
            CancellationToken cancellationToken)
            => _metadataStore.StoreInstanceMetadataAsync(dicomDataset, version, cancellationToken);
    }
}
