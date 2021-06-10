// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Features.Operations
{
    /// <summary>
    /// Represents a client for interacting with DICOM-specific Azure Durable Orchestrations.
    /// </summary>
    internal class DicomDurableFunctionsHttpClient : IDicomOperationsClient
    {
        private readonly HttpClient _client;
        private readonly OperationsConfiguration _config;
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="DicomDurableFunctionsHttpClient"/> class.
        /// </summary>
        /// <param name="client">The HTTP client used to communicate with the HTTP triggered functions.</param>
        /// <param name="config">A configuration that specifies how to communicate with the Azure Functions.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="client"/>, <paramref name="config"/>, or the value of the configuration is <see langword="null"/>.
        /// </exception>
        public DicomDurableFunctionsHttpClient(HttpClient client, IOptions<OperationsConfiguration> config)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(config?.Value, nameof(config));

            client.BaseAddress = config.Value.BaseAddress;
            client.DefaultRequestHeaders.Add(HeaderNames.Accept, MediaTypeNames.Application.Json);

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

            using HttpResponseMessage response = await _client.GetAsync(statusRoute, cancellationToken);
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
    }
}
