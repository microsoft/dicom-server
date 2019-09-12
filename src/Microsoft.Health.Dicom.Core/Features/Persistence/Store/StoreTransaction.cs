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
using Microsoft.Health.Dicom.Core.Features.Transaction;

namespace Microsoft.Health.Dicom.Core.Features.Persistence.Store
{
    public class StoreTransaction : IDisposable
    {
        /// <summary>
        /// The list of value representations that will not be stored when creating the 'metadata' storage objects.
        /// </summary>
        private static readonly HashSet<DicomVR> OtherAndUnkownValueRepresentations = new HashSet<DicomVR>()
        {
            DicomVR.OB,
            DicomVR.OD,
            DicomVR.OF,
            DicomVR.OL,
            DicomVR.OW,
            DicomVR.UN,
        };

        private readonly IDicomTransactionService _transactionService;
        private readonly IDicomBlobDataStore _dicomBlobDataStore;
        private readonly IDicomMetadataStore _dicomMetadataStore;
        private readonly IDicomInstanceMetadataStore _dicomInstanceMetadataStore;
        private readonly IDicomIndexDataStore _dicomIndexDataStore;
        private readonly IDictionary<DicomInstance, DicomDataset> _metadataInstances = new Dictionary<DicomInstance, DicomDataset>();
        private ITransaction _transaction;
        private bool _disposed;

        public StoreTransaction(
            IDicomTransactionService transactionService,
            IDicomBlobDataStore dicomBlobDataStore,
            IDicomMetadataStore dicomMetadataStore,
            IDicomInstanceMetadataStore dicomInstanceMetadataStore,
            IDicomIndexDataStore dicomIndexDataStore)
        {
            EnsureArg.IsNotNull(transactionService, nameof(transactionService));
            EnsureArg.IsNotNull(dicomBlobDataStore, nameof(dicomBlobDataStore));
            EnsureArg.IsNotNull(dicomMetadataStore, nameof(dicomMetadataStore));
            EnsureArg.IsNotNull(dicomInstanceMetadataStore, nameof(dicomInstanceMetadataStore));
            EnsureArg.IsNotNull(dicomIndexDataStore, nameof(dicomIndexDataStore));

            _transactionService = transactionService;
            _dicomBlobDataStore = dicomBlobDataStore;
            _dicomMetadataStore = dicomMetadataStore;
            _dicomInstanceMetadataStore = dicomInstanceMetadataStore;
            _dicomIndexDataStore = dicomIndexDataStore;
        }

        public async Task StoreDicomFileAsync(Stream dicomFileStream, DicomFile dicomFile, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomFileStream, nameof(dicomFileStream));
            EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));

            var dicomInstance = DicomInstance.Create(dicomFile.Dataset);

            // Attempt to create a transaction, or append to an existing transaction.
            if (_transaction == null)
            {
                await _transactionService.CreateTransactionAsync(dicomInstance, cancellationToken);
            }
            else
            {
                await _transaction.AppendInstanceAsync(dicomInstance, cancellationToken);
            }

            // If a file with the same name exists, a conflict exception will be thrown.
            dicomFileStream.Seek(0, SeekOrigin.Begin);
            await _dicomBlobDataStore.AddInstanceAsStreamAsync(dicomInstance, dicomFileStream, cancellationToken: cancellationToken);

            // Strip the DICOM file down to the tags we want to store for metadata.
            RemoveOtherAndUnknownValueRepresentations(dicomFile.Dataset);
            _metadataInstances.Add(dicomInstance, dicomFile.Dataset);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task CommitAsync()
        {
            if (_metadataInstances.Count == 0)
            {
                return;
            }

            // Commit per series grouping
            foreach (IGrouping<string, KeyValuePair<DicomInstance, DicomDataset>> grouping in _metadataInstances.GroupBy(x => x.Key.StudyInstanceUID + x.Key.SeriesInstanceUID))
            {
                DicomDataset[] seriesArray = grouping.Select(x => x.Value).ToArray();
                foreach (DicomDataset metadataInstance in seriesArray)
                {
                    await _dicomInstanceMetadataStore.AddInstanceMetadataAsync(metadataInstance);
                }

                await _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(seriesArray);
                await _dicomIndexDataStore.IndexSeriesAsync(seriesArray);
            }

            _metadataInstances.Clear();
            await _transaction.CommitAsync();
        }

        public async Task AbortAsync()
        {
            if (_metadataInstances.Count == 0)
            {
                return;
            }

            // Abort per series grouping
            foreach (IGrouping<string, KeyValuePair<DicomInstance, DicomDataset>> grouping in _metadataInstances.GroupBy(x => x.Key.StudyInstanceUID + x.Key.SeriesInstanceUID))
            {
                DicomInstance[] instances = grouping.Select(x => x.Key).ToArray();

                // Attempt to delete the instance indexes and instance metadata.
                await Task.WhenAll(
                    _dicomIndexDataStore.DeleteInstancesIndexAsync(throwOnNotFound: false, dicomInstances: instances),
                    _dicomMetadataStore.DeleteInstanceAsync(throwOnNotFound: false, dicomInstances: instances));

                await Task.WhenAll(instances.Select(async x =>
                {
                    await _dicomInstanceMetadataStore.DeleteInstanceMetadataAsync(x);
                    await _dicomBlobDataStore.DeleteInstanceIfExistsAsync(x);
                }));
            }

            // If we managed to clear-up, we commit the transaction.
            _metadataInstances.Clear();
            await _transaction.CommitAsync();
        }

        internal static void RemoveOtherAndUnknownValueRepresentations(DicomDataset dicomDataset)
        {
            var tagsToRemove = new List<DicomTag>();
            foreach (DicomItem item in dicomDataset)
            {
                if (item.ValueRepresentation == DicomVR.SQ && item is DicomSequence sequence)
                {
                    foreach (DicomDataset sequenceDataset in sequence.Items)
                    {
                        RemoveOtherAndUnknownValueRepresentations(sequenceDataset);
                    }
                }
                else if (OtherAndUnkownValueRepresentations.Contains(item.ValueRepresentation))
                {
                    tagsToRemove.Add(item.Tag);
                }
            }

            dicomDataset.Remove(tagsToRemove.ToArray());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                try
                {
                    Task.WaitAll(AbortAsync());
                }
                catch
                {
                    // Ignore any exceptions from an abort
                }

                _transaction?.Dispose();
            }

            _disposed = true;
        }
    }
}
