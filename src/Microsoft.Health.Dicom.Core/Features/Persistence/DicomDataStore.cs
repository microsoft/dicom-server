// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public class DicomDataStore : IDicomDataStore
    {
        // This value should not be changed without taking into consideration concurrency.
        private const bool OverwriteBlobsIfExists = false;
        private readonly IDicomBlobDataStore _dicomBlobDataStore;
        private readonly IDicomIndexDataStore _dicomIndexDataStore;

        public DicomDataStore(
            IDicomBlobDataStore dicomBlobDataStore,
            IDicomIndexDataStore dicomIndexDataStore)
        {
            EnsureArg.IsNotNull(dicomBlobDataStore, nameof(dicomBlobDataStore));
            EnsureArg.IsNotNull(dicomIndexDataStore, nameof(dicomIndexDataStore));

            _dicomBlobDataStore = dicomBlobDataStore;
            _dicomIndexDataStore = dicomIndexDataStore;
        }

        public async Task StoreAsync(DicomFile dicomFile, CancellationToken cancellationToken = default)
        {
            var dicomIdentity = DicomIdentity.Create(dicomFile.Dataset);
            var instanceBlobName = GetInstanceBlobName(dicomIdentity);

            // Step 1. Store the blob file
            using (var memoryStream = new MemoryStream())
            {
                await dicomFile.SaveAsync(memoryStream);
                await _dicomBlobDataStore.AddFileAsStreamAsync(instanceBlobName, memoryStream, overwriteIfExists: OverwriteBlobsIfExists, cancellationToken);
            }

            // Step 2. Attempt to index the file; this will fail if it exists, which should never happen.
            await _dicomIndexDataStore.IndexInstanceAsync(dicomFile.Dataset, cancellationToken);
        }

        public async Task DeleteInstanceAsync(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, CancellationToken cancellationToken = default)
        {
            var instanceBlobName = GetInstanceBlobName(studyInstanceUID, seriesInstanceUID, sopInstanceUID);

            // Reverse process of store.
            // 1. Delete the instance index
            await _dicomIndexDataStore.DeleteInstanceIndexAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, cancellationToken);

            // 2. Delete the instance file.
            await _dicomBlobDataStore.DeleteFileIfExistsAsync(instanceBlobName, cancellationToken);
        }

        private static string GetInstanceBlobName(DicomIdentity dicomIdentity)
            => GetInstanceBlobName(dicomIdentity.StudyInstanceUID, dicomIdentity.SeriesInstanceUID, dicomIdentity.SopInstanceUID);

        private static string GetInstanceBlobName(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID)
            => $"{studyInstanceUID}\\{seriesInstanceUID}\\{sopInstanceUID}";
    }
}
