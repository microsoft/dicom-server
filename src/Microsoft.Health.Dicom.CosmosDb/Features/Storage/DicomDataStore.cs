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
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Core;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Storage
{
    public class DicomDataStore : IDicomDataStore
    {
        private const bool OverwriteBlobs = false;
        private readonly IDicomBlobDataStore _blobDataStore;

        public DicomDataStore(IDicomBlobDataStore blobDataStore)
        {
            EnsureArg.IsNotNull(blobDataStore, nameof(_blobDataStore));

            _blobDataStore = blobDataStore;
        }

        public async Task<StoreOutcome> StoreDicomFileAsync(Stream stream, string studyInstanceUID = null, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(stream, nameof(stream));
            EnsureArg.IsTrue(stream.CanSeek, nameof(stream.CanSeek));

            DicomIdentity dicomIdentity = null;

            try
            {
                DicomFile dicomFile = await DicomFile.OpenAsync(stream);
                dicomIdentity = new DicomIdentity(dicomFile.Dataset);

                bool isDicomIdentityValid =
                    !string.IsNullOrWhiteSpace(dicomIdentity.StudyInstanceUID) &&
                    !string.IsNullOrWhiteSpace(dicomIdentity.SeriesInstanceUID) &&
                    !string.IsNullOrWhiteSpace(dicomIdentity.SopInstanceUID) &&
                    !string.IsNullOrWhiteSpace(dicomIdentity.SopClassUID) &&
                    (studyInstanceUID == null || studyInstanceUID == dicomIdentity.StudyInstanceUID);

                if (!isDicomIdentityValid)
                {
                    return new StoreOutcome(false, dicomIdentity);
                }

                string blobName = GetDicomRawBlobName(dicomIdentity);
                stream.Seek(0, SeekOrigin.Begin);
                await _blobDataStore.AddFileAsStreamAsync(blobName, stream, overwriteIfExists: OverwriteBlobs, cancellationToken);
            }
            catch (Exception ex)
            {
                return new StoreOutcome(false, dicomIdentity, ex);
            }

            return new StoreOutcome(true, dicomIdentity);
        }

        private static string GetDicomRawBlobName(DicomIdentity dicomIdentity)
        {
            EnsureArg.IsNotNull(dicomIdentity);
            return $"{dicomIdentity.StudyInstanceUID}\\{dicomIdentity.SeriesInstanceUID}\\{dicomIdentity.SopInstanceUID}";
        }
    }
}
