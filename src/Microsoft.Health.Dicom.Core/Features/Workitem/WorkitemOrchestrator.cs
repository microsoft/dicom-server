// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Query;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to orchestrate the storing of the DICOM instance entry.
    /// </summary>
    public class WorkitemOrchestrator : IWorkitemOrchestrator
    {
        private readonly IDicomRequestContextAccessor _contextAccessor;
        private readonly IIndexWorkitemStore _indexWorkitemStore;
        private readonly IWorkitemStore _workitemStore;

        public WorkitemOrchestrator(
            IDicomRequestContextAccessor contextAccessor,
            IWorkitemStore workitemStore,
            IIndexWorkitemStore indexWorkitemStore,
            IDeleteService deleteService,
            IQueryTagService queryTagService)
        {
            _contextAccessor = EnsureArg.IsNotNull(contextAccessor, nameof(contextAccessor));
            _indexWorkitemStore = EnsureArg.IsNotNull(indexWorkitemStore, nameof(indexWorkitemStore));
            _workitemStore = EnsureArg.IsNotNull(workitemStore, nameof(workitemStore));
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

                var queryTags = QueryLimit.IndexedWorkitemQueryTags;

                long workitemKey = await _indexWorkitemStore
                    .AddWorkitemAsync(partitionKey, dataset, QueryLimit.IndexedWorkitemQueryTags, cancellationToken)
                    .ConfigureAwait(false);

                // TODO: this needs to be done before storing the dataset in the Blob Store.
                string workitemInstanceUid = dataset.GetString(DicomTag.SOPInstanceUID);
                dataset.Add(DicomTag.RequestedSOPInstanceUID, workitemInstanceUid);

                identifier = dataset.ToWorkitemInstanceIdentifier(workitemKey, partitionKey);

                // We have successfully created the index, store the file.
                await StoreWorkitemBlobAsync(identifier, dataset, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                // TODO: implement cleanup to delete the index if blob storage fails
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
