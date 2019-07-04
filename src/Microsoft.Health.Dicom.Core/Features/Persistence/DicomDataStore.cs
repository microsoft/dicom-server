// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public class DicomDataStore
    {
        private const bool OverwriteBlobIfExists = false;
        private readonly IDicomBlobDataStore _blobDataStore;
        private readonly IDicomMetadataStore _metadataStore;
        private readonly IDicomIndexDataStore _indexDataStore;

        public DicomDataStore(
            IDicomBlobDataStore blobDataStore,
            IDicomMetadataStore metadataStore,
            IDicomIndexDataStore indexDataStore)
        {
            EnsureArg.IsNotNull(blobDataStore, nameof(blobDataStore));
            EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
            EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));

            _blobDataStore = blobDataStore;
            _metadataStore = metadataStore;
            _indexDataStore = indexDataStore;
        }

        public async Task StoreAsync(Stream rawBuffer, DicomFile dicomFile, CancellationToken cancellationToken = default)
        {
            var dicomInstance = DicomInstance.Create(dicomFile.Dataset);
            var instanceBlobName = GetInstanceBlobName(dicomInstance);

            if (rawBuffer.Position != 0 && rawBuffer.CanSeek)
            {
                rawBuffer.Seek(0, SeekOrigin.Begin);
            }

            // TODO: Add some locking and fault tolerance between blob, metadata and index writes.
            await _blobDataStore.AddFileAsStreamAsync(instanceBlobName, rawBuffer, overwriteIfExists: OverwriteBlobIfExists, cancellationToken);
            await _metadataStore.AddStudySeriesDicomMetadataAsync(dicomFile.Dataset, cancellationToken);
            await _indexDataStore.IndexInstanceAsync(dicomFile.Dataset, cancellationToken);
        }

        public async Task<IEnumerable<Stream>> OpenStudyReadAsync(string studyInstanceUID, CancellationToken cancellationToken = default)
        {
            IEnumerable<DicomInstance> instancesInSeries = await _metadataStore.GetInstancesInStudyAsync(studyInstanceUID, cancellationToken);
            IEnumerable<string> blobNames = instancesInSeries.Select(x => GetInstanceBlobName(x));

            return await Task.WhenAll(blobNames.Select(x => _blobDataStore.GetFileAsStreamAsync(x, cancellationToken)));
        }

        public async Task<IEnumerable<Stream>> OpenSeriesReadAsync(string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken = default)
        {
            IEnumerable<DicomInstance> instancesInSeries = await _metadataStore.GetInstancesInSeriesAsync(studyInstanceUID, seriesInstanceUID, cancellationToken);
            IEnumerable<string> blobNames = instancesInSeries.Select(x => GetInstanceBlobName(x));

            return await Task.WhenAll(blobNames.Select(x => _blobDataStore.GetFileAsStreamAsync(x, cancellationToken)));
        }

        public async Task<Stream> OpenInstanceReadAsync(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, CancellationToken cancellationToken = default)
        {
            var instanceBlobName = GetInstanceBlobName(studyInstanceUID, seriesInstanceUID, sopInstanceUID);
            return await _blobDataStore.GetFileAsStreamAsync(instanceBlobName, cancellationToken);
        }

        private static string GetInstanceBlobName(DicomInstance dicomInstance)
        {
            return GetInstanceBlobName(dicomInstance.StudyInstanceUID, dicomInstance.SeriesInstanceUID, dicomInstance.SopInstanceUID);
        }

        private static string GetInstanceBlobName(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID)
        {
            return $"{studyInstanceUID}\\{seriesInstanceUID}\\{sopInstanceUID}";
        }
    }
}
