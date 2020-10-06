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

namespace Microsoft.Health.DicomCast.Core.Features.DicomWeb.Service
{
    /// <summary>
    /// Provides functionality to retrieve the change feed from DICOMWeb.
    /// </summary>
    public class ChangeFeedRetrieveService : IChangeFeedRetrieveService
    {
        private const int DefaultLimit = 10;

        private readonly IDicomWebClient _dicomWebClient;

        public ChangeFeedRetrieveService(IDicomWebClient dicomWebClient)
        {
            EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));

            _dicomWebClient = dicomWebClient;
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ChangeFeedEntry>> RetrieveChangeFeedAsync(long offset, CancellationToken cancellationToken)
        {
            DicomWebAsyncEnumerableResponse<ChangeFeedEntry> result = await _dicomWebClient.GetChangeFeed(
                $"?offset={offset}&limit={DefaultLimit}&includeMetadata={true}",
                cancellationToken);

            ChangeFeedEntry[] changeFeedEntries = await result.ToArrayAsync();

            if (changeFeedEntries?.Any() != null)
            {
                return changeFeedEntries;
            }

            return Array.Empty<ChangeFeedEntry>();
        }
    }
}
