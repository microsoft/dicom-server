// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.Dicom.Client;

public partial class DicomWebClient : IDicomWebClient
{
    public async Task<DicomWebResponse<T>> ResolveReferenceAsync<T>(IResourceReference<T> resourceReference, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(resourceReference, nameof(resourceReference));

        using var request = new HttpRequestMessage(HttpMethod.Get, resourceReference.Href);
        HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);
        return new DicomWebResponse<T>(response, JsonSerializerOptions);
    }
}
