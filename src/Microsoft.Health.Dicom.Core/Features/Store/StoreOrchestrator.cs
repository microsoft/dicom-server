// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to orchestrate the storing of the DICOM instance entry.
    /// </summary>
    public class StoreOrchestrator : IStoreOrchestrator
    {
        private readonly IFileStore _blobDataStore;
        private readonly IMetadataStore _instanceMetadataStore;
        private readonly IIndexDataStore _indexDataStore;

        public StoreOrchestrator(
            IFileStore blobDataStore,
            IMetadataStore instanceMetadataStore,
            IIndexDataStore indexDataStore)
        {
            EnsureArg.IsNotNull(blobDataStore, nameof(blobDataStore));
            EnsureArg.IsNotNull(instanceMetadataStore, nameof(instanceMetadataStore));
            EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));

            _blobDataStore = blobDataStore;
            _instanceMetadataStore = instanceMetadataStore;
            _indexDataStore = indexDataStore;
        }

        /// <inheritdoc />
        public async Task StoreDicomInstanceEntryAsync(
            IInstanceEntry instanceEntry,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(instanceEntry, nameof(instanceEntry));

            DicomDataset dicomDataset = await instanceEntry.GetDicomDatasetAsync(cancellationToken);

            long version = await _indexDataStore.CreateInstanceIndexAsync(dicomDataset, cancellationToken);

            VersionedInstanceIdentifier instanceIdentifier = dicomDataset.ToVersionedDicomInstanceIdentifier(version);

            try
            {
                // We have successfully created the index, store the files.
                Task[] tasks = new[]
                {
                    StoreBlobAsync(instanceIdentifier, instanceEntry, cancellationToken),
                    StoreInstanceMetadataAsync(dicomDataset, version, cancellationToken),
                };

                await Task.WhenAll(tasks);

                // Successfully uploaded the files. Update the status to be available.
                await _indexDataStore.UpdateInstanceIndexStatusAsync(instanceIdentifier, IndexStatus.Created, cancellationToken);
            }
            catch (Exception)
            {
                // Exception occurred while storing the file. Try delete the index.
                _ = Task.Run(() => TryCleanupInstanceIndexAsync(instanceIdentifier));
                throw;
            }
        }

        private async Task StoreBlobAsync(
            VersionedInstanceIdentifier instanceIdentifier,
            IInstanceEntry instanceEntry,
            CancellationToken cancellationToken)
        {
            Stream stream = await instanceEntry.GetStreamAsync(cancellationToken);

            await _blobDataStore.AddFileAsync(
                instanceIdentifier,
                stream,
                cancellationToken: cancellationToken);
        }

        private Task StoreInstanceMetadataAsync(
            DicomDataset dicomDataset,
            long version,
            CancellationToken cancellationToken)
            => _instanceMetadataStore.AddInstanceMetadataAsync(dicomDataset, version, cancellationToken);

        private async Task TryCleanupInstanceIndexAsync(VersionedInstanceIdentifier instanceIdentifier)
        {
            try
            {
                // In case the request is canceled and one of the operation failed, we still want to cleanup.
                // Therefore, we will not be using the same cancellation token as the request itself.
                await _indexDataStore.DeleteInstanceIndexAsync(instanceIdentifier, CancellationToken.None);
            }
            catch (Exception)
            {
                // Fire and forget.
            }
        }
    }
}
