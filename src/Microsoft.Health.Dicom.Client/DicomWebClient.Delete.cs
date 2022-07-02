// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Dicom.Client;

public partial class DicomWebClient : IDicomWebClient
{
    public Task<DicomWebResponse> DeleteStudyAsync(
        string studyInstanceUid,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

        return DeleteAsync(
            GenerateRequestUri(string.Format(CultureInfo.InvariantCulture, DicomWebConstants.BaseStudyUriFormat, studyInstanceUid), partitionName),
            cancellationToken);
    }

    public Task<DicomWebResponse> DeleteSeriesAsync(
        string studyInstanceUid,
        string seriesInstanceUid,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
        EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));

        return DeleteAsync(
            GenerateRequestUri(string.Format(CultureInfo.InvariantCulture, DicomWebConstants.BaseSeriesUriFormat, studyInstanceUid, seriesInstanceUid), partitionName),
            cancellationToken);
    }

    public Task<DicomWebResponse> DeleteInstanceAsync(
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
        EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
        EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));


        return DeleteAsync(
            GenerateRequestUri(string.Format(CultureInfo.InvariantCulture, DicomWebConstants.BaseInstanceUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid), partitionName),
            cancellationToken);
    }

    private async Task<DicomWebResponse> DeleteAsync(Uri requestUri, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);

        HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

        return new DicomWebResponse(response);
    }
}
