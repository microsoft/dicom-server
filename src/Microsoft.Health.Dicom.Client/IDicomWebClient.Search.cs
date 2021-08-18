// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.Dicom.Client
{
    public partial interface IDicomWebClient
    {
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryInstancesAsync(string queryString, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QuerySeriesAsync(string queryString, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QuerySeriesInstanceAsync(string seriesInstanceUid, string queryString, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudyAsync(string queryString, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudyInstanceAsync(string studyInstanceUid, string queryString, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudySeriesAsync(string studyInstanceUid, string queryString, CancellationToken cancellationToken = default);
        Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudySeriesInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string queryString, CancellationToken cancellationToken = default);
    }
}
