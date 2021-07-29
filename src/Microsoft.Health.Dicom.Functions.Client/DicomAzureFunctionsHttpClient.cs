// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Functions.Client.Configs;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Functions.Client
{
    /// <summary>
    /// Represents a client for interacting with DICOM-specific Azure Functions.
    /// </summary>
    internal class DicomAzureFunctionsHttpClient : IDicomOperationsClient
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly FunctionsClientOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="DicomAzureFunctionsHttpClient"/> class.
        /// </summary>
        /// <param name="client">The HTTP client used to communicate with the HTTP triggered functions.</param>
        /// <param name="jsonSerializerOptions">Settings to be used when serializing or deserializing JSON.</param>
        /// <param name="options">A configuration that specifies how to communicate with the Azure Functions.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="client"/>, <paramref name="jsonSerializerOptions"/>, <paramref name="options"/>, or
        /// either <see cref="IOptions{TOptions}.Value"/> is <see langword="null"/>.
        /// </exception>
        public DicomAzureFunctionsHttpClient(
            HttpClient client,
            IOptions<JsonSerializerOptions> jsonSerializerOptions,
            IOptions<FunctionsClientOptions> options)
        {
            _client = EnsureArg.IsNotNull(client, nameof(client));
            _jsonSerializerOptions = EnsureArg.IsNotNull(jsonSerializerOptions?.Value, nameof(jsonSerializerOptions));
            _options = EnsureArg.IsNotNull(options?.Value, nameof(options));

            client.BaseAddress = options.Value.BaseAddress;
        }

        /// <inheritdoc/>
        public async Task<OperationStatusResponse> GetStatusAsync(string operationId, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(operationId, nameof(operationId));

            var statusRoute = new Uri(
                string.Format(CultureInfo.InvariantCulture, _options.Routes.GetStatusRouteTemplate, operationId),
                UriKind.Relative);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, statusRoute);
            request.Headers.Add(HeaderNames.Accept, MediaTypeNames.Application.Json);

            using HttpResponseMessage response = await _client.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            // Re-throw any exceptions we may have encountered when making the HTTP request
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<OperationStatusResponse>(_jsonSerializerOptions, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<string> StartQueryTagIndexingAsync(IReadOnlyCollection<int> tagKeys, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(tagKeys, nameof(tagKeys));
            EnsureArg.HasItems(tagKeys, nameof(tagKeys));

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                _options.Routes.StartQueryTagIndexingRoute,
                tagKeys,
                _jsonSerializerOptions,
                cancellationToken);

            // If there is a conflict, another client already added this tag while we were processing
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new ExtendedQueryTagsAlreadyExistsException();
            }

            // Re-throw any exceptions we may have encountered when making the HTTP request
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
    }
}
