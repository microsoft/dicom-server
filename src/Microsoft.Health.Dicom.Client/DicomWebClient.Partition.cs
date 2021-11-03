// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.Dicom.Client
{
    public partial class DicomWebClient : IDicomWebClient
    {
        public async Task<DicomWebResponse<IEnumerable<PartitionEntry>>> GetPartitionsAsync(CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                GenerateRequestUri(DicomWebConstants.PartitionsUriString));

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return new DicomWebResponse<IEnumerable<PartitionEntry>>(response, ValueFactory<IEnumerable<PartitionEntry>>);
        }
    }
}
