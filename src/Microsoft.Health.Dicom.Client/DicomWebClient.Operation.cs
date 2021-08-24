// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.Dicom.Client
{
    public partial class DicomWebClient : IDicomWebClient
    {
        public async Task<DicomWebResponse<OperationStatus>> GetOperationStatusAsync(string operationId, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(operationId, nameof(operationId));
            var uri = new Uri($"/{_apiVersion}{DicomWebConstants.BaseOperationUri}/{operationId}", UriKind.Relative);
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
            return new DicomWebResponse<OperationStatus>(response, ValueFactory<OperationStatus>);
        }
    }
}
