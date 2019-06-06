// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
        private readonly IDicomBlobDataStore _blobDataStore;

        public DicomDataStore(IDicomBlobDataStore blobDataStore)
        {
            EnsureArg.IsNotNull(blobDataStore, nameof(_blobDataStore));

            _blobDataStore = blobDataStore;
        }

        /// <inheritdoc />
        public Task<bool> StoreDicomFileAsync(DicomFile dicomFile, string studyInstanceUID = null, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));

            var dicomIdentity = new DicomIdentity(dicomFile.Dataset);

            bool isDicomIdentityValid =
                !string.IsNullOrWhiteSpace(dicomIdentity.StudyInstanceUID) &&
                !string.IsNullOrWhiteSpace(dicomIdentity.SeriesInstanceUID) &&
                !string.IsNullOrWhiteSpace(dicomIdentity.SopInstanceUID) &&
                !string.IsNullOrWhiteSpace(dicomIdentity.SopClassUID) &&
                (studyInstanceUID == null || studyInstanceUID == dicomIdentity.StudyInstanceUID);

            if (!isDicomIdentityValid)
            {
                return Task.FromResult(false);
            }

            // TODO: Store the provided DICOM file and index it.
            return Task.FromResult(true);
        }
    }
}
