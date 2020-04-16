// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Core.Features.Delete
{
    public class DicomDeleteService : IDicomDeleteService
    {
        private readonly IDicomIndexDataStore _dicomIndexDataStore;

        public DicomDeleteService(IDicomIndexDataStore dicomIndexDataStore)
        {
            EnsureArg.IsNotNull(dicomIndexDataStore, nameof(dicomIndexDataStore));

            _dicomIndexDataStore = dicomIndexDataStore;
        }

        public async Task DeleteStudyAsync(string studyInstanceUid, CancellationToken cancellationToken)
        {
            await _dicomIndexDataStore.DeleteStudyIndexAsync(studyInstanceUid, cancellationToken);
        }

        public async Task DeleteSeriesAsync(string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken)
        {
            await _dicomIndexDataStore.DeleteSeriesIndexAsync(studyInstanceUid, seriesInstanceUid, cancellationToken);
        }

        public async Task DeleteInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken)
        {
            await _dicomIndexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, cancellationToken);
        }
    }
}
