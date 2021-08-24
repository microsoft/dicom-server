// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.Dicom.Client
{
    public partial interface IDicomWebClient
    {
        Task<DicomWebAsyncEnumerableResponse<ChangeFeedEntry>> GetChangeFeed(string queryString = "", CancellationToken cancellationToken = default);
        Task<DicomWebResponse<ChangeFeedEntry>> GetChangeFeedLatest(string queryString = "", CancellationToken cancellationToken = default);
    }
}
