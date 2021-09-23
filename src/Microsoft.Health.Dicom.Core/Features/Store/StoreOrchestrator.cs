// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Polly;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to orchestrate the storing of the DICOM instance entry.
    /// </summary>
    public class StoreOrchestrator : IStoreOrchestrator
    {
        private readonly IDicomRequestContextAccessor _contextAccessor;
        private readonly IFileStore _fileStore;
        private readonly IMetadataStore _metadataStore;
        private readonly IIndexDataStore _indexDataStore;
        private readonly IDeleteService _deleteService;
        private readonly IQueryTagService _queryTagService;
        private readonly AsyncPolicy _updatePolicy;

        public event EventHandler<QueryTagsExpiredEventArgs> QueryTagsExpired;

        public StoreOrchestrator(
            IDicomRequestContextAccessor contextAccessor,
            IFileStore fileStore,
            IMetadataStore metadataStore,
            IIndexDataStore indexDataStore,
            IDeleteService deleteService,
            IQueryTagService queryTagService,
            IOptions<StoreConfiguration> storeConfiguration)
        {
            _contextAccessor = EnsureArg.IsNotNull(contextAccessor, nameof(contextAccessor));
            _fileStore = EnsureArg.IsNotNull(fileStore, nameof(fileStore));
            _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
            _indexDataStore = EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
            _deleteService = EnsureArg.IsNotNull(deleteService, nameof(deleteService));
            _queryTagService = EnsureArg.IsNotNull(queryTagService, nameof(queryTagService));

            StoreConfiguration config = EnsureArg.IsNotNull(storeConfiguration?.Value, nameof(storeConfiguration));
            _updatePolicy = Policy
                .Handle<ExtendedQueryTagsOutOfDateException>()
                .RetryAsync(config.MaxRetriesWhenTagsOutOfDate);
        }

        /// <inheritdoc />
        public async Task StoreDicomInstanceEntryAsync(
            IDicomInstanceEntry dicomInstanceEntry,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dicomInstanceEntry, nameof(dicomInstanceEntry));
            var partitionId = _contextAccessor.RequestContext?.PartitionId;
            DicomDataset dicomDataset = await dicomInstanceEntry.GetDicomDatasetAsync(cancellationToken);

            IReadOnlyCollection<QueryTag> queryTags = await _queryTagService.GetQueryTagsAsync(forceRefresh: false, cancellationToken: cancellationToken);
            long watermark = await _indexDataStore.BeginCreateInstanceIndexAsync(partitionId, dicomDataset, queryTags, cancellationToken);
            var versionedInstanceIdentifier = dicomDataset.ToVersionedInstanceIdentifier(watermark, partitionId);

            try
            {
                // We have successfully created the index, store the files.
                await Task.WhenAll(
                    StoreFileAsync(versionedInstanceIdentifier, dicomInstanceEntry, cancellationToken),
                    StoreInstanceMetadataAsync(dicomDataset, watermark, cancellationToken));

                await EndAddInstanceIndexAsync(partitionId, dicomDataset, watermark, cancellationToken);
            }
            catch (Exception)
            {
                // Exception occurred while storing the file. Try delete the index.
                await TryCleanupInstanceIndexAsync(versionedInstanceIdentifier);
                throw;
            }
        }

        private async Task StoreFileAsync(
            VersionedInstanceIdentifier versionedInstanceIdentifier,
            IDicomInstanceEntry dicomInstanceEntry,
            CancellationToken cancellationToken)
        {
            Stream stream = await dicomInstanceEntry.GetStreamAsync(cancellationToken);

            await _fileStore.StoreFileAsync(
                versionedInstanceIdentifier,
                stream,
                cancellationToken);
        }

        private Task StoreInstanceMetadataAsync(
            DicomDataset dicomDataset,
            long version,
            CancellationToken cancellationToken)
            => _metadataStore.StoreInstanceMetadataAsync(dicomDataset, version, cancellationToken);

        private Task EndAddInstanceIndexAsync(
            string partitionId,
            DicomDataset dicomDataset,
            long watermark,
            CancellationToken cancellationToken)
        {
            // Retry when new extended query tags have been added
            return _updatePolicy.ExecuteAsync(
                async token =>
                {
                    IReadOnlyCollection<QueryTag> queryTags = await _queryTagService.GetQueryTagsAsync(forceRefresh: false, cancellationToken: token);

                    try
                    {
                        await _indexDataStore.EndCreateInstanceIndexAsync(partitionId, dicomDataset, watermark, queryTags, cancellationToken: token);
                    }
                    catch (ExtendedQueryTagsOutOfDateException)
                    {
                        // Determine which tags have been added
                        IReadOnlyCollection<QueryTag> newTags = await _queryTagService.GetQueryTagsAsync(forceRefresh: true, cancellationToken: token);
                        Dictionary<string, QueryTag> difference = newTags.ToDictionary(x => x.Tag.GetPath(), StringComparer.OrdinalIgnoreCase);
                        foreach (QueryTag oldTag in queryTags)
                        {
                            difference.Remove(oldTag.Tag.GetPath());
                        }

                        OnQueryTagsExpired(new QueryTagsExpiredEventArgs { DicomDataset = dicomDataset, NewQueryTags = difference.Values });
                        throw;
                    }
                },
                cancellationToken);
        }

        protected virtual void OnQueryTagsExpired(QueryTagsExpiredEventArgs e)
            => QueryTagsExpired?.Invoke(this, e);

        private async Task TryCleanupInstanceIndexAsync(VersionedInstanceIdentifier versionedInstanceIdentifier)
        {
            try
            {
                // In case the request is canceled and one of the operation failed, we still want to cleanup.
                // Therefore, we will not be using the same cancellation token as the request itself.
                await _deleteService.DeleteInstanceNowAsync(
                    versionedInstanceIdentifier.StudyInstanceUid,
                    versionedInstanceIdentifier.SeriesInstanceUid,
                    versionedInstanceIdentifier.SopInstanceUid,
                    CancellationToken.None);
            }
            catch (Exception)
            {
                // Fire and forget.
            }
        }
    }
}
