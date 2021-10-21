// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dicom;

namespace Microsoft.Health.Dicom.Client
{
    public partial interface IDicomWebClient
    {
        Task<DicomWebResponse<DicomDataset>> StoreAsync(DicomFile dicomFile, string partitionName = null, string studyInstanceUid = null, CancellationToken cancellationToken = default);
        Task<DicomWebResponse<DicomDataset>> StoreAsync(HttpContent content, string partitionName = null, CancellationToken cancellationToken = default);
        Task<DicomWebResponse<DicomDataset>> StoreAsync(IEnumerable<DicomFile> dicomFiles, string partitionName = null, string studyInstanceUid = null, CancellationToken cancellationToken = default);
        Task<DicomWebResponse<DicomDataset>> StoreAsync(IEnumerable<Stream> streams, string partitionName = null, string studyInstanceUid = null, CancellationToken cancellationToken = default);
        Task<DicomWebResponse<DicomDataset>> StoreAsync(Stream stream, string partitionName = null, string studyInstanceUid = null, CancellationToken cancellationToken = default);
    }
}
