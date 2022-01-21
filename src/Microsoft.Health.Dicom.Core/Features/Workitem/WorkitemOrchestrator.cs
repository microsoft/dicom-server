// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to orchestrate the DICOM workitem instance add, retrieve, cancel, and update.
    /// </summary>
    public class WorkitemOrchestrator : IWorkitemOrchestrator
    {
        private readonly IDicomRequestContextAccessor _contextAccessor;
        private readonly IIndexWorkitemStore _indexWorkitemStore;
        private readonly IWorkitemStore _workitemStore;
        private readonly IWorkitemQueryTagService _workitemQueryTagService;

        public WorkitemOrchestrator(
            IDicomRequestContextAccessor contextAccessor,
            IWorkitemStore workitemStore,
            IIndexWorkitemStore indexWorkitemStore,
            IDeleteService deleteService,
            IWorkitemQueryTagService workitemQueryTagService)
        {
            _contextAccessor = EnsureArg.IsNotNull(contextAccessor, nameof(contextAccessor));
            _indexWorkitemStore = EnsureArg.IsNotNull(indexWorkitemStore, nameof(indexWorkitemStore));
            _workitemStore = EnsureArg.IsNotNull(workitemStore, nameof(workitemStore));
            _workitemQueryTagService = workitemQueryTagService;
        }

        /// <inheritdoc />
        public async Task AddWorkitemAsync(
            DicomDataset dataset,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));

            WorkitemInstanceIdentifier identifier = null;

            try
            {
                int partitionKey = _contextAccessor.RequestContext.GetPartitionKey();

                IReadOnlyCollection<QueryTag> queryTags = await _workitemQueryTagService.GetQueryTagsAsync(cancellationToken).ConfigureAwait(false);

                long workitemKey = await _indexWorkitemStore
                    .AddWorkitemAsync(partitionKey, dataset, queryTags, cancellationToken)
                    .ConfigureAwait(false);

                string workitemInstanceUid = dataset.GetString(DicomTag.AffectedSOPInstanceUID);
                dataset.Add(DicomTag.RequestedSOPInstanceUID, workitemInstanceUid);

                identifier = dataset.ToWorkitemInstanceIdentifier(workitemKey, partitionKey);

                // We have successfully created the index, store the file.
                await StoreWorkitemBlobAsync(identifier, dataset, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                await TryDeleteWorkitemAsync(identifier, cancellationToken).ConfigureAwait(false);

                throw;
            }
        }

        /// <inheritdoc />
        private async Task TryDeleteWorkitemAsync(
            WorkitemInstanceIdentifier identifier,
            CancellationToken cancellationToken)
        {
            if (null == identifier)
            {
                return;
            }

            try
            {
                await _indexWorkitemStore
                    .DeleteWorkitemAsync(identifier.PartitionKey, identifier.WorkitemUid, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
            }
        }

        private async Task StoreWorkitemBlobAsync(
            WorkitemInstanceIdentifier identifier,
            DicomDataset dicomDataset,
            CancellationToken cancellationToken)
            => await _workitemStore
                .AddWorkitemAsync(identifier, dicomDataset, cancellationToken)
                .ConfigureAwait(false);
    }
}
