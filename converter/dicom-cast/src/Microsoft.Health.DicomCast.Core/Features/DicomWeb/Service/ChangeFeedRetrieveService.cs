// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.DicomCast.Core.Features.DicomWeb.Service;

/// <summary>
/// Provides functionality to retrieve the change feed from DICOMWeb.
/// </summary>
public class ChangeFeedRetrieveService : IChangeFeedRetrieveService
{
    private readonly IDicomWebClient _dicomWebClient;

    public ChangeFeedRetrieveService(IDicomWebClient dicomWebClient)
    {
        _dicomWebClient = EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ChangeFeedEntry>> RetrieveChangeFeedAsync(long offset, int limit, CancellationToken cancellationToken = default)
    {
        using DicomWebAsyncEnumerableResponse<ChangeFeedEntry> result = await _dicomWebClient.GetChangeFeed(
            $"?offset={offset}&limit={limit}&includeMetadata=true",
            cancellationToken);

        return await result.ToArrayAsync(cancellationToken) ?? Array.Empty<ChangeFeedEntry>();
    }

    public async Task<long> RetrieveLatestSequenceAsync(CancellationToken cancellationToken = default)
    {
        using DicomWebResponse<ChangeFeedEntry> response = await _dicomWebClient.GetChangeFeedLatest("?includeMetadata=false", cancellationToken);
        ChangeFeedEntry latest = await response.GetValueAsync();

        return latest?.Sequence ?? 0L; // 0L is the default offset used by the Change Feed and SyncStateStore
    }
}
