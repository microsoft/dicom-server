// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
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
        private readonly IDictionary<string, ITransaction> _transactions = new Dictionary<string, ITransaction>();
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
            var dicomSeries = new DicomSeries(dicomInstance.StudyInstanceUID, dicomInstance.SeriesInstanceUID);

            // Attempt to create a lock for the series.
            ITransaction transaction = await GetTransactionAsync(dicomSeries, cancellationToken);

            // Validate if the instance exists, if true throw conflict exception.
            if (await _dicomBlobDataStore.InstanceExistsAsync(dicomInstance, cancellationToken))
            {
                throw new DataStoreException(HttpStatusCode.Conflict);
            }

            // Now append to the transaction we are about to modify this instance.
            await transaction.AppendInstanceAsync(dicomInstance, cancellationToken);

            // Write the instance to blob storage.
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

            // Commit per transaction
            foreach (ITransaction transaction in _transactions.Values)
            {
                DicomDataset[] datasets = transaction.Message.Instances.Select(x => _metadataInstances[x]).ToArray();
                foreach (DicomDataset instanceDataset in datasets)
                {
                    await _dicomInstanceMetadataStore.AddInstanceMetadataAsync(instanceDataset);
                }

                await _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(datasets);
                await _dicomIndexDataStore.IndexSeriesAsync(datasets);

                await transaction.CommitAsync();
            }

            _metadataInstances.Clear();
        }

        public async Task AbortAsync()
        {
            if (_metadataInstances.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<string, ITransaction> transaction in _transactions)
            {
                await transaction.Value.Message.DeleteInstancesAsync(_dicomBlobDataStore, _dicomMetadataStore, _dicomInstanceMetadataStore, _dicomIndexDataStore);
                await transaction.Value.CommitAsync();
            }

            _metadataInstances.Clear();
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

                _transactions.Values.Each(x => x.Dispose());
            }

            _disposed = true;
        }

        private async Task<ITransaction> GetTransactionAsync(DicomSeries dicomSeries, CancellationToken cancellationToken = default)
        {
            var transactionId = dicomSeries.StudyInstanceUID + dicomSeries.SeriesInstanceUID;

            if (!_transactions.TryGetValue(transactionId, out ITransaction transaction))
            {
                transaction = await _transactionService.BeginTransactionAsync(dicomSeries, cancellationToken);
                _transactions[transactionId] = transaction;
            }

            return transaction;
        }
    }
}
