// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Polly;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to orchestrate the storing of the DICOM instance entry.
    /// </summary>
    public class StoreOrchestrator : IStoreOrchestrator
    {
        private readonly IFileStore _fileStore;
        private readonly IMetadataStore _metadataStore;
        private readonly IIndexDataStore _indexDataStore;
        private readonly IDeleteService _deleteService;
        private readonly IQueryTagService _queryTagService;
        private readonly IElementMinimumValidator _minimumValidator;
        private readonly AsyncPolicy _updatePolicy;

        public StoreOrchestrator(
            IFileStore fileStore,
            IMetadataStore metadataStore,
            IIndexDataStore indexDataStore,
            IDeleteService deleteService,
            IQueryTagService queryTagService,
            IElementMinimumValidator minimumValidator,
            IOptions<StoreConfiguration> storeConfiguration)
        {
            _fileStore = EnsureArg.IsNotNull(fileStore, nameof(fileStore));
            _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
            _indexDataStore = EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
            _deleteService = EnsureArg.IsNotNull(deleteService, nameof(deleteService));
            _queryTagService = EnsureArg.IsNotNull(queryTagService, nameof(queryTagService));
            _minimumValidator = EnsureArg.IsNotNull(minimumValidator, nameof(minimumValidator));

            StoreConfiguration config = EnsureArg.IsNotNull(storeConfiguration?.Value, nameof(storeConfiguration));
            _updatePolicy = Policy
                .Handle<ExtendedQueryTagVersionMismatchException>()
                .RetryAsync(config.MaxRetriesWhenTagVersionMismatch, (e, r, cxt) => OnRetryUpdate(cxt));
        }

        /// <inheritdoc />
        public async Task StoreDicomInstanceEntryAsync(
            IDicomInstanceEntry dicomInstanceEntry,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dicomInstanceEntry, nameof(dicomInstanceEntry));

            DicomDataset dicomDataset = await dicomInstanceEntry.GetDicomDatasetAsync(cancellationToken);

            // Retry when max ExtendedQuryTagVersion mismatch.
            IReadOnlyCollection<QueryTag> queryTags = await _queryTagService.GetQueryTagsAsync(forceRefresh: false, cancellationToken: cancellationToken);
            long watermark = await _indexDataStore.BeginCreateInstanceIndexAsync(dicomDataset, queryTags, cancellationToken);
            var versionedInstanceIdentifier = dicomDataset.ToVersionedInstanceIdentifier(watermark);

            try
            {
                // We have successfully created the index, store the files.
                await Task.WhenAll(
                    StoreFileAsync(versionedInstanceIdentifier, dicomInstanceEntry, cancellationToken),
                    StoreInstanceMetadataAsync(dicomDataset, watermark, cancellationToken));

                await EndAddInstanceIndexAsync(dicomDataset, watermark, queryTags, cancellationToken);
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

        private async Task EndAddInstanceIndexAsync(
            DicomDataset dicomDataset,
            long watermark,
            IReadOnlyCollection<QueryTag> queryTags,
            CancellationToken cancellationToken)
        {
            await _updatePolicy.ExecuteAsync(
                (context, token) => _indexDataStore.EndCreateInstanceIndexAsync(
                    context[nameof(DicomDataset)] as DicomDataset,
                    watermark,
                    context[nameof(QueryTag)] as IReadOnlyCollection<QueryTag>,
                    token),
                new Dictionary<string, object>
                {
                    { nameof(DicomDataset), dicomDataset },
                    { nameof(CancellationToken), cancellationToken },
                    { nameof(QueryTag), queryTags },
                },
                cancellationToken);
        }

        private async Task OnRetryUpdate(Polly.Context context)
        {
            // Bypass the cache with forceRefresh: true
            CancellationToken token = (CancellationToken)context[nameof(CancellationToken)];
            IReadOnlyCollection<QueryTag> queryTags = await _queryTagService.GetQueryTagsAsync(forceRefresh: true, cancellationToken: token);

            // Re-validate
            DicomDataset dicomDataset = context[nameof(DicomDataset)] as DicomDataset;
            foreach (QueryTag queryTag in queryTags)
            {
                dicomDataset.ValidateQueryTag(queryTag, _minimumValidator);
            }

            // Update context with latest query tag values
            context[nameof(QueryTag)] = queryTags;
        }

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
