// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.IO.Writer;
using Microsoft.Health.Dicom.Client.Http;

namespace Microsoft.Health.Dicom.Client;

public partial class DicomWebClient : IDicomWebClient
{
    public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
        IEnumerable<DicomFile> dicomFiles,
        string studyInstanceUid,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(dicomFiles, nameof(dicomFiles));

        using MultipartContent content = DicomContent.CreateMultipart(dicomFiles);
        return await StoreAsync(
            GenerateStoreRequestUri(partitionName, studyInstanceUid),
            content,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
        IEnumerable<Stream> streams,
        string studyInstanceUid,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(streams, nameof(streams));

        using MultipartContent content = CreateMultipartDicomStreamContent(streams);
        return await StoreAsync(
            GenerateStoreRequestUri(partitionName, studyInstanceUid),
            content,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
        Stream stream,
        string studyInstanceUid,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(stream, nameof(stream));

        stream.Seek(0, SeekOrigin.Begin);

        return await StoreAsync(
            GenerateStoreRequestUri(partitionName, studyInstanceUid),
            CreateDicomStreamContent(stream),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
        DicomFile dicomFile,
        string studyInstanceUid,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));

        return await StoreAsync(
            GenerateStoreRequestUri(partitionName, studyInstanceUid),
            new DicomContent(dicomFile),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
        HttpContent content,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        return await StoreAsync(
            GenerateStoreRequestUri(partitionName),
            content,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<DicomWebResponse<DicomDataset>> StoreAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(requestUri, nameof(requestUri));
        EnsureArg.IsNotNull(content, nameof(content));

        using HttpRequestMessage request = new(HttpMethod.Post, requestUri) { Content = content };
        request.Headers.Accept.Add(DicomWebConstants.MediaTypeApplicationDicomJson);

        HttpResponseMessage response = await HttpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessStatusCodeAsync(
            response,
            additionalFailureInspector: (statusCode, responseHeaders, contentHeaders, responseBody) =>
            {
                // If store fails, we will get Conflict status code but the body will be a DicomDataset,
                // so we need to handle this case specially.
                if (statusCode == HttpStatusCode.Conflict)
                {
                    throw new DicomWebException(
                        statusCode,
                        responseHeaders,
                        contentHeaders,
                        JsonSerializer.Deserialize<DicomDataset>(responseBody, JsonSerializerOptions));
                }

                return false;
            }).ConfigureAwait(false);

        return new DicomWebResponse<DicomDataset>(response, ValueFactory<DicomDataset>);
    }

    private static StreamContent CreateDicomStreamContent(Stream stream)
    {
        StreamContent content = new(stream);
        content.Headers.ContentType = DicomWebConstants.MediaTypeApplicationDicom;

        return content;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Callers will dispose of the StreamContent")]
    private static MultipartContent CreateMultipartDicomStreamContent(IEnumerable<Stream> streams, DicomWriteOptions options = null)
    {
        MultipartContent content = new("related");
        content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("type", $"\"{DicomWebConstants.MediaTypeApplicationDicom.MediaType}\""));

        foreach (Stream stream in streams)
            content.Add(CreateDicomStreamContent(stream));

        return content;
    }
}
