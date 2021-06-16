// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Functions.Client.Configs;
using Microsoft.Health.Dicom.Functions.Client.Models;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Functions.Client
{
    /// <summary>
    /// Represents a client for interacting with DICOM-specific Azure Functions.
    /// </summary>
    internal class DicomAzureFunctionsHttpClient : IDicomOperationsClient
    {
        private readonly HttpClient _client;
        private readonly FunctionsClientConfiguration _config;
        internal static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="DicomAzureFunctionsHttpClient"/> class.
        /// </summary>
        /// <param name="client">The HTTP client used to communicate with the HTTP triggered functions.</param>
        /// <param name="config">A configuration that specifies how to communicate with the Azure Functions.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="client"/>, <paramref name="config"/>, or the value of the configuration is <see langword="null"/>.
        /// </exception>
        public DicomAzureFunctionsHttpClient(HttpClient client, IOptions<FunctionsClientConfiguration> config)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(config?.Value, nameof(config));

            client.BaseAddress = config.Value.BaseAddress;

            _client = client;
            _config = config.Value;
        }

        /// <inheritdoc/>
        public async Task<OperationStatusResponse> GetStatusAsync(string operationId, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(operationId, nameof(operationId));

            var statusRoute = new Uri(
                string.Format(CultureInfo.InvariantCulture, _config.Routes.StatusTemplate, operationId),
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

            DurableOrchestrationInstanceStatus responseState = JsonConvert.DeserializeObject<DurableOrchestrationInstanceStatus>(
#if NET5_0_OR_GREATER
                await response.Content.ReadAsStringAsync(cancellationToken),
#else
                await response.Content.ReadAsStringAsync(),
#endif
                JsonSettings);

            return new OperationStatusResponse(
                responseState.InstanceId,
                responseState.Type,
                responseState.CreatedTime,
                responseState.RuntimeStatus);
        }

        /// <inheritdoc/>
        public async Task<string> StartExtendedQueryTagAdditionAsync(IReadOnlyCollection<AddExtendedQueryTagEntry> tags, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(tags, nameof(tags));
            EnsureArg.HasItems(tags, nameof(tags));

            using var content = new StringContent(JsonConvert.SerializeObject(tags, JsonSettings), Encoding.UTF8, MediaTypeNames.Application.Json);
            using HttpResponseMessage response = await _client.PostAsync(_config.Routes.StartQueryTagReindex, content, cancellationToken);

            // Re-throw any exceptions we may have encountered when making the HTTP request
            response.EnsureSuccessStatusCode();

            string instanceId =
#if NET5_0_OR_GREATER
                await response.Content.ReadAsStringAsync(cancellationToken);
#else
                await response.Content.ReadAsStringAsync();
#endif

            return instanceId;
        }
    }
}
