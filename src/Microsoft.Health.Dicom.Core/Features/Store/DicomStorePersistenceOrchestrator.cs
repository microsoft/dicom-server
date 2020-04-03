// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Store.Upload;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to orchestrate persisting the DICOM resource to datastores.
    /// </summary>
    public class DicomStorePersistenceOrchestrator : IDicomStorePersistenceOrchestrator
    {
        private readonly IDicomFileStore _dicomBlobDataStore;
        private readonly IDicomMetadataStore _dicomInstanceMetadataStore;
        private readonly IDicomIndexDataStore _dicomIndexDataStore;
        private readonly ILogger _logger;

        public DicomStorePersistenceOrchestrator(
            IDicomFileStore dicomBlobDataStore,
            IDicomMetadataStore dicomInstanceMetadataStore,
            IDicomIndexDataStore dicomIndexDataStore,
            ILogger<DicomStorePersistenceOrchestrator> logger)
        {
            EnsureArg.IsNotNull(dicomBlobDataStore, nameof(dicomBlobDataStore));
            EnsureArg.IsNotNull(dicomInstanceMetadataStore, nameof(dicomInstanceMetadataStore));
            EnsureArg.IsNotNull(dicomIndexDataStore, nameof(dicomIndexDataStore));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _dicomBlobDataStore = dicomBlobDataStore;
            _dicomInstanceMetadataStore = dicomInstanceMetadataStore;
            _dicomIndexDataStore = dicomIndexDataStore;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task PersistUploadedDicomInstanceAsync(IUploadedDicomInstance uploadedDicomResource, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(uploadedDicomResource, nameof(uploadedDicomResource));

            DicomDataset dicomDataset = await uploadedDicomResource.GetDicomDatasetAsync(cancellationToken);
            Stream stream = await uploadedDicomResource.GetStreamAsync(cancellationToken);

            // If a file with the same name exists, a conflict exception will be thrown.
            await _dicomBlobDataStore.AddAsync(
                dicomDataset.ToDicomInstanceIdentifier(),
                stream,
                cancellationToken: cancellationToken);

            // Strip the DICOM file down to the tags we want to store for metadata.
            dicomDataset.RemoveBulkDataVrs();

            await _dicomInstanceMetadataStore.AddInstanceMetadataAsync(dicomDataset);
            await _dicomIndexDataStore.IndexInstanceAsync(dicomDataset);
        }
    }
}
