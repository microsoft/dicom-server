// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.Dicom.Client;

public partial class DicomWebClient : IDicomWebClient
{
    public async Task<DicomWebResponse<DicomOperationReference>> StartExportAsync(
        ExportSource source,
        ExportDestination destination,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(source, nameof(source));
        EnsureArg.IsNotNull(destination, nameof(destination));

        string jsonString = JsonSerializer.Serialize(
            new ExportSpecification
            {
                Destination = destination,
                Source = source,
            },
            JsonSerializerOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, GenerateRequestUri(DicomWebConstants.ExportUriString, partitionName));
        {
            request.Content = new StringContent(jsonString);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(DicomWebConstants.ApplicationJsonMediaType);
        }

        HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
        return new DicomWebResponse<DicomOperationReference>(response, ValueFactory<DicomOperationReference>);
    }
}
