// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Core.Models.Update;

namespace Microsoft.Health.Dicom.Client;

public partial class DicomWebClient : IDicomWebClient
{
    public async Task<DicomWebResponse> UpdateAsync(
        IReadOnlyList<string> studyInstanceUids,
        DicomDataset dataset,
        int partitionKey = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(studyInstanceUids, nameof(studyInstanceUids));
        EnsureArg.IsNotNull(dataset, nameof(dataset));

        string jsonString = JsonSerializer.Serialize(
            new UpdateSpecification(studyInstanceUids, dataset),
            JsonSerializerOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, GenerateUpdateRequestUri());
        {
            request.Content = new StringContent(jsonString);
            request.Content.Headers.ContentType = DicomWebConstants.MediaTypeApplicationJson;
        }

        HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
        return new DicomWebResponse<DicomOperationReference>(response, ValueFactory<DicomOperationReference>);
    }
}
