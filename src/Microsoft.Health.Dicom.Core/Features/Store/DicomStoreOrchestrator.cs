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
using Microsoft.Health.Core;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to orchestrate the storing of the DICOM instance entry.
    /// </summary>
    public class DicomStoreOrchestrator : IDicomStoreOrchestrator
    {
        private readonly IDicomFileStore _dicomBlobDataStore;
        private readonly IDicomMetadataStore _dicomInstanceMetadataStore;
        private readonly IDicomIndexDataStore _dicomIndexDataStore;

        public DicomStoreOrchestrator(
            IDicomFileStore dicomBlobDataStore,
            IDicomMetadataStore dicomInstanceMetadataStore,
            IDicomIndexDataStore dicomIndexDataStore)
        {
            EnsureArg.IsNotNull(dicomBlobDataStore, nameof(dicomBlobDataStore));
            EnsureArg.IsNotNull(dicomInstanceMetadataStore, nameof(dicomInstanceMetadataStore));
            EnsureArg.IsNotNull(dicomIndexDataStore, nameof(dicomIndexDataStore));

            _dicomBlobDataStore = dicomBlobDataStore;
            _dicomInstanceMetadataStore = dicomInstanceMetadataStore;
            _dicomIndexDataStore = dicomIndexDataStore;
        }

        /// <inheritdoc />
        public async Task StoreDicomInstanceEntryAsync(
            IDicomInstanceEntry dicomInstanceEntry,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dicomInstanceEntry, nameof(dicomInstanceEntry));

            DicomDataset dicomDataset = await dicomInstanceEntry.GetDicomDatasetAsync(cancellationToken);

            long version = await _dicomIndexDataStore.CreateInstanceIndexAsync(dicomDataset, cancellationToken);

            VersionedDicomInstanceIdentifier dicomInstanceIdentifier = dicomDataset.ToVersionedDicomInstanceIdentifier(version);

            try
            {
                // We have successfully created the index, store the files.
                Task[] tasks = new[]
                {
                    StoreBlobAsync(dicomInstanceIdentifier, dicomInstanceEntry, cancellationToken),
                    StoreInstanceMetadataAsync(dicomDataset, version, cancellationToken),
                };

                await Task.WhenAll(tasks);

                // Successfully uploaded the files. Update the status to be available.
                await _dicomIndexDataStore.UpdateInstanceIndexStatusAsync(dicomInstanceIdentifier, DicomIndexStatus.Created, cancellationToken);
            }
            catch (Exception)
            {
                // Exception occurred while storing the file. Try delete the index.
                _ = Task.Run(() => TryCleanupInstanceIndexAsync(dicomInstanceIdentifier));
                throw;
            }
        }

        private async Task StoreBlobAsync(
            VersionedDicomInstanceIdentifier dicomInstanceIdentifier,
            IDicomInstanceEntry dicomInstanceEntry,
            CancellationToken cancellationToken)
        {
            Stream stream = await dicomInstanceEntry.GetStreamAsync(cancellationToken);

            await _dicomBlobDataStore.AddFileAsync(
                dicomInstanceIdentifier,
                stream,
                cancellationToken: cancellationToken);
        }

        private Task StoreInstanceMetadataAsync(
            DicomDataset dicomDataset,
            long version,
            CancellationToken cancellationToken)
            => _dicomInstanceMetadataStore.AddInstanceMetadataAsync(dicomDataset, version, cancellationToken);

        private async Task TryCleanupInstanceIndexAsync(VersionedDicomInstanceIdentifier dicomInstanceIdentifier)
        {
            try
            {
                // In case the request is canceled and one of the operation failed, we still want to cleanup.
                // Therefore, we will not be using the same cancellation token as the request itself.
                await _dicomIndexDataStore.DeleteInstanceIndexAsync(dicomInstanceIdentifier, CancellationToken.None);
            }
            catch (Exception)
            {
                // Fire and forget.
            }
        }
    }
}
