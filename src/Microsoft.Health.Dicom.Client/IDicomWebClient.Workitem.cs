// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Client;

public partial interface IDicomWebClient
{
    Task<DicomWebResponse> AddWorkitemAsync(IEnumerable<DicomDataset> dicomDatasets, string workitemUid = default, string partitionName = default, CancellationToken cancellationToken = default);

    Task<DicomWebResponse> CancelWorkitemAsync(IEnumerable<DicomDataset> dicomDatasets, string workitemUid, string partitionName = default, CancellationToken cancellationToken = default);

    Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryWorkitemAsync(string queryString, string partitionName = default, CancellationToken cancellationToken = default);

    Task<DicomWebResponse<DicomDataset>> RetrieveWorkitemAsync(string workitemUid, string partitionName = default, CancellationToken cancellationToken = default);

    Task<DicomWebResponse> ChangeWorkitemStateAsync(IEnumerable<DicomDataset> dicomDatasets, string workitemUid, string partitionName = default, CancellationToken cancellationToken = default);
}
