// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Functions.Client.Configs;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Functions.Client
{
    /// <summary>
    /// Represents a client for interacting with DICOM-specific Azure Functions.
    /// </summary>
    internal class DicomAzureFunctionsHttpClient : IDicomOperationsClient
    {
        internal const string FunctionAccessKeyHeader = "x-functions-key";

        private readonly HttpClient _client;
        private readonly IUrlResolver _urlResolver;
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly FunctionsClientOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="DicomAzureFunctionsHttpClient"/> class.
        /// </summary>
        /// <param name="client">The HTTP client used to communicate with the HTTP triggered functions.</param>
        /// <param name="urlResolver">A helper for building URLs for other APIs.</param>
        /// <param name="extendedQueryTagStore">An extended query tag store for resolving the query tag IDs.</param>
        /// <param name="jsonSerializerOptions">Settings to be used when serializing or deserializing JSON.</param>
        /// <param name="options">A configuration that specifies how to communicate with the Azure Functions.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="client"/>, <paramref name="urlResolver"/>, <paramref name="jsonSerializerOptions"/>,
        /// <paramref name="options"/>, or its <see cref="IOptions{TOptions}.Value"/> is <see langword="null"/>.
        /// </exception>
        public DicomAzureFunctionsHttpClient(
            HttpClient client,
            IUrlResolver urlResolver,
            IExtendedQueryTagStore extendedQueryTagStore,
            IOptions<JsonSerializerOptions> jsonSerializerOptions,
            IOptions<FunctionsClientOptions> options)
        {
            _client = EnsureArg.IsNotNull(client, nameof(client));
            _urlResolver = EnsureArg.IsNotNull(urlResolver, nameof(urlResolver));
            _extendedQueryTagStore = EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            _jsonSerializerOptions = EnsureArg.IsNotNull(jsonSerializerOptions?.Value, nameof(jsonSerializerOptions));
            _options = EnsureArg.IsNotNull(options?.Value, nameof(options));

            client.BaseAddress = options.Value.BaseAddress;

            if (!string.IsNullOrEmpty(_options.FunctionAccessKey))
            {
                client.DefaultRequestHeaders.Add(FunctionAccessKeyHeader, _options.FunctionAccessKey);
            }
        }

        /// <inheritdoc/>
        public async Task<OperationStatus> GetStatusAsync(Guid operationId, CancellationToken cancellationToken = default)
        {
            var statusRoute = new Uri(
                string.Format(CultureInfo.InvariantCulture, _options.Routes.GetStatusRouteTemplate, OperationId.ToString(operationId)),
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
            InternalOperationStatus status = await response.Content.ReadFromJsonAsync<InternalOperationStatus>(_jsonSerializerOptions, cancellationToken);
            return new OperationStatus
            {
                CreatedTime = status.CreatedTime,
                LastUpdatedTime = status.LastUpdatedTime,
                OperationId = status.OperationId,
                PercentComplete = status.PercentComplete,
                Resources = await GetResourceUrlsAsync(status.Type, status.ResourceIds, cancellationToken),
                Status = status.Status,
                Type = status.Type,
            };
        }

        /// <inheritdoc/>
        public async Task<Guid> StartQueryTagIndexingAsync(IReadOnlyCollection<int> tagKeys, CancellationToken cancellationToken = default)
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
            return Guid.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        }

        private async Task<IReadOnlyCollection<Uri>> GetResourceUrlsAsync(
            OperationType type,
            IReadOnlyCollection<string> resourceIds,
            CancellationToken cancellationToken)
        {
            switch (type)
            {
                case OperationType.Reindex:
                    List<int> tagKeys = resourceIds?.Select(x => int.Parse(x, CultureInfo.InvariantCulture)).ToList();

                    IReadOnlyCollection<ExtendedQueryTagStoreEntry> tagPaths = Array.Empty<ExtendedQueryTagStoreEntry>();
                    if (tagKeys?.Count > 0)
                    {
                        tagPaths = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(tagKeys, cancellationToken);
                    }

                    return tagPaths.Select(x => _urlResolver.ResolveQueryTagUri(x.Path)).ToList();
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
