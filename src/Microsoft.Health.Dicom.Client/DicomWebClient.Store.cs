// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Client
{
    public partial class DicomWebClient : IDicomWebClient
    {
        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
            IEnumerable<DicomFile> dicomFiles,
            string partitionName,
            string studyInstanceUid,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dicomFiles, nameof(dicomFiles));

            var postContent = new List<Stream>();

            try
            {
                foreach (DicomFile dicomFile in dicomFiles)
                {
                    MemoryStream stream = GetMemoryStream();
                    await dicomFile.SaveAsync(stream).ConfigureAwait(false);
                    stream.Seek(0, SeekOrigin.Begin);
                    postContent.Add(stream);
                }

                using MultipartContent content = ConvertStreamsToMultipartContent(postContent);
                return await StoreAsync(
                    GenerateStoreRequestUri(partitionName, studyInstanceUid),
                    content,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                foreach (Stream stream in postContent)
                {
                    await stream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
            IEnumerable<Stream> streams,
            string partitionName,
            string studyInstanceUid,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(streams, nameof(streams));

            return await StoreAsync(
                GenerateStoreRequestUri(partitionName, studyInstanceUid),
                ConvertStreamsToMultipartContent(streams),
                cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
            Stream stream,
            string partitionName,
            string studyInstanceUid,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(stream, nameof(stream));

            stream.Seek(0, SeekOrigin.Begin);

            return await StoreAsync(
                GenerateStoreRequestUri(partitionName, studyInstanceUid),
                ConvertStreamToStreamContent(stream),
                cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
            DicomFile dicomFile,
            string partitionName,
            string studyInstanceUid,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));

            await using MemoryStream stream = GetMemoryStream();
            await dicomFile.SaveAsync(stream).ConfigureAwait(false);
            stream.Seek(0, SeekOrigin.Begin);

            using StreamContent content = ConvertStreamToStreamContent(stream);
            return await StoreAsync(
                GenerateStoreRequestUri(partitionName, studyInstanceUid),
                content,
                cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(
            HttpContent content,
            string partitionName,
            CancellationToken cancellationToken)
        {
            return await StoreAsync(
                GenerateStoreRequestUri(partitionName),
                content,
                cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<DicomWebResponse<DicomDataset>> StoreAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(requestUri, nameof(requestUri));
            EnsureArg.IsNotNull(content, nameof(content));

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);

            request.Headers.Accept.Add(DicomWebConstants.MediaTypeApplicationDicomJson);

            request.Content = content;

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
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
                            JsonConvert.DeserializeObject<DicomDataset>(responseBody, _jsonSerializerSettings));
                    }

                    return false;
                })
                .ConfigureAwait(false);

            return new DicomWebResponse<DicomDataset>(response, ValueFactory<DicomDataset>);
        }
    }
}
