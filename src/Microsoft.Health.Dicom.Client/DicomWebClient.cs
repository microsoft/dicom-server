// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.FellowOakDicom.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Health.Dicom.Client.Serialization;
using Microsoft.Net.Http.Headers;

using MediaTypeHeaderValue = Microsoft.Net.Http.Headers.MediaTypeHeaderValue;
using NameValueHeaderValue = System.Net.Http.Headers.NameValueHeaderValue;

namespace Microsoft.Health.Dicom.Client;

[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Callers are responsible for disposing of return values.")]
public partial class DicomWebClient : IDicomWebClient
{
    internal static readonly JsonSerializerOptions JsonSerializerOptions = CreateJsonSerializerOptions();

    /// <summary>
    /// New instance of DicomWebClient to talk to the server
    /// </summary>
    /// <param name="httpClient">HttpClient</param>
    /// <param name="apiVersion">Pin the DicomWebClient to a specific server API version.</param>
    public DicomWebClient(HttpClient httpClient, string apiVersion = DicomApiVersions.Latest)
    {
        EnsureArg.IsNotNull(httpClient, nameof(httpClient));

        HttpClient = httpClient;
        ApiVersion = apiVersion;
        GetMemoryStream = () => new MemoryStream();
    }

    public HttpClient HttpClient { get; }

    public string ApiVersion { get; }

    /// <summary>
    /// Func used to obtain a <see cref="MemoryStream" />. The default value returns a new memory stream.
    /// </summary>
    /// <remarks>
    /// This can be used in conjunction with a memory stream pool.
    /// </remarks>
    public Func<MemoryStream> GetMemoryStream { get; set; }

    private static async Task<T> ValueFactory<T>(HttpContent content)
    {
        string contentText = await content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(contentText, JsonSerializerOptions);
    }

    private static string FormatQueryString(string queryString)
        => string.IsNullOrWhiteSpace(queryString) ? string.Empty : "?" + queryString;

    private static string CreateAcceptHeader(MediaTypeWithQualityHeaderValue mediaTypeHeader, string dicomTransferSyntax)
    {
        string transferSyntaxHeader = dicomTransferSyntax == null ? string.Empty : $";{DicomWebConstants.TransferSyntaxHeaderName}=\"{dicomTransferSyntax}\"";

        return $"{mediaTypeHeader}{transferSyntaxHeader}";
    }

    private static MediaTypeWithQualityHeaderValue CreateMultipartMediaTypeHeader(string contentType)
    {
        var multipartHeader = new MediaTypeWithQualityHeaderValue(DicomWebConstants.MultipartRelatedMediaType);
        var contentHeader = new NameValueHeaderValue("type", "\"" + contentType + "\"");

        multipartHeader.Parameters.Add(contentHeader);
        return multipartHeader;
    }

    private Uri GenerateRequestUri(string relativePath, string partitionName = default)
    {
        var uriString = "/" + ApiVersion;

        if (!string.IsNullOrEmpty(partitionName))
        {
            uriString += string.Format(CultureInfo.InvariantCulture, DicomWebConstants.BasePartitionUriFormat, partitionName);
        }

        uriString += relativePath;

        return new Uri(uriString, UriKind.Relative);
    }

    private Uri GenerateStoreRequestUri(string partitionName = default, string studyInstanceUid = default)
    {
        if (string.IsNullOrEmpty(studyInstanceUid))
        {
            return GenerateRequestUri(DicomWebConstants.StudiesUriString, partitionName);
        }

        return GenerateRequestUri(string.Format(CultureInfo.InvariantCulture, DicomWebConstants.BaseStudyUriFormat, studyInstanceUid), partitionName);
    }

    private Uri GenerateWorkitemAddRequestUri(string workitemUid, string partitionName = default)
    {
        return GenerateRequestUri(string.Format(CultureInfo.InvariantCulture, DicomWebConstants.AddWorkitemUriFormat, workitemUid), partitionName);
    }

    private Uri GenerateWorkitemCancelRequestUri(string workitemUid, string partitionName = default)
    {
        return GenerateRequestUri(string.Format(CultureInfo.InvariantCulture, DicomWebConstants.CancelWorkitemUriFormat, workitemUid), partitionName);
    }

    private Uri GenerateWorkitemRetrieveRequestUri(string workitemUid, string partitionName = default)
    {
        return GenerateRequestUri(string.Format(CultureInfo.InvariantCulture, DicomWebConstants.BaseWorkitemUriFormat, workitemUid), partitionName);
    }

    private Uri GenerateChangeWorkitemStateRequestUri(string workitemUid, string partitionName = default)
    {
        return GenerateRequestUri(string.Format(CultureInfo.InvariantCulture, DicomWebConstants.ChangeWorkitemStateUriFormat, workitemUid), partitionName);
    }

    private Uri GenerateWorkitemUpdateRequestUri(string workitemUid, string transactionUid, string partitionName = default)
    {
        return GenerateRequestUri(string.Format(CultureInfo.InvariantCulture, DicomWebConstants.UpdateWorkitemUriFormat, workitemUid, transactionUid), partitionName);
    }

    private Uri GenerateUpdateRequestUri(string partitionName = default)
    {
        return GenerateRequestUri(DicomWebConstants.UpdateAttributeUriString, partitionName);
    }

    private async IAsyncEnumerable<Stream> ReadMultipartResponseAsStreamsAsync(HttpContent httpContent, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(httpContent, nameof(httpContent));

        using Stream stream = await httpContent
#if NETSTANDARD2_0
            .ReadAsStreamAsync()
#else
            .ReadAsStreamAsync(cancellationToken)
#endif
            .ConfigureAwait(false);
        stream.Seek(0, SeekOrigin.Begin);
        MultipartSection part;

        var media = MediaTypeHeaderValue.Parse(httpContent.Headers.ContentType.ToString());
        var multipartReader = new MultipartReader(HeaderUtilities.RemoveQuotes(media.Boundary).Value, stream, 100);

        while ((part = await multipartReader.ReadNextSectionAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            MemoryStream memoryStream = GetMemoryStream();
            await part.Body
#if NETSTANDARD2_0
                .CopyToAsync(memoryStream)
#else
                .CopyToAsync(memoryStream, cancellationToken)
#endif
                .ConfigureAwait(false);

            memoryStream.Seek(0, SeekOrigin.Begin);

            yield return memoryStream;
        }
    }

    private static async Task EnsureSuccessStatusCodeAsync(
        HttpResponseMessage response,
        Func<HttpStatusCode, HttpResponseHeaders, HttpContentHeaders, string, bool> additionalFailureInspector = null)
    {
        if (!response.IsSuccessStatusCode)
        {
            HttpStatusCode statusCode = response.StatusCode;
            HttpResponseHeaders responseHeaders = response.Headers;
            HttpContentHeaders contentHeaders = response.Content?.Headers;
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Exception exception = null;

            try
            {
                bool handled = additionalFailureInspector?.Invoke(statusCode, responseHeaders, contentHeaders, responseBody) ?? false;

                if (!handled)
                {
                    throw new DicomWebException(statusCode, responseHeaders, contentHeaders, responseBody);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                // If we are throwing exception, then we can close the response because we have already read the body.
                if (exception != null)
                {
                    response.Dispose();
                }
            }
        }
    }

    private async IAsyncEnumerable<DicomFile> ReadMultipartResponseAsDicomFileAsync(HttpContent content, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (Stream stream in ReadMultipartResponseAsStreamsAsync(content, cancellationToken).ConfigureAwait(false))
        {
            yield return await DicomFile.OpenAsync(stream).ConfigureAwait(false);
        }
    }

    private static async IAsyncEnumerable<T> DeserializeAsAsyncEnumerable<T>(HttpContent content)
    {
        string contentText = await content.ReadAsStringAsync().ConfigureAwait(false);

        if (string.IsNullOrEmpty(contentText))
        {
            yield break;
        }

        foreach (T item in JsonSerializer.Deserialize<IReadOnlyList<T>>(contentText, JsonSerializerOptions))
        {
            yield return item;
        }
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            Encoder = null,
            IgnoreReadOnlyFields = false,
            IgnoreReadOnlyProperties = false,
            IncludeFields = false,
            MaxDepth = 0, // 0 indicates the max depth of 64
            NumberHandling = JsonNumberHandling.Strict,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = false,
        };

        options.Converters.Add(new DicomIdentifierJsonConverter());
        options.Converters.Add(new DicomJsonConverter(writeTagsAsKeywords: true, autoValidate: false, numberSerializationMode: NumberSerializationMode.PreferablyAsNumber));
        options.Converters.Add(new ExportDataOptionsJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new OperationStateConverter());

        return options;
    }
}
