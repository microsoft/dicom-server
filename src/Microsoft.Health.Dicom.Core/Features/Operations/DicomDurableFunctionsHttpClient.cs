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
    internal class DicomDurableFunctionsHttpClient : IDicomOperationsClient
    {
        private readonly HttpClient _client;
        private readonly OperationsConfiguration _config;
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        };

        public DicomDurableFunctionsHttpClient(HttpClient client, IOptions<OperationsConfiguration> config)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(config?.Value, nameof(config));

            client.BaseAddress = config.Value.BaseAddress;
            client.DefaultRequestHeaders.Add(HeaderNames.Accept, MediaTypeNames.Application.Json);

            _client = client;
            _config = config.Value;
        }

        public async Task<OperationStatusResponse> GetStatusAsync(string id, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(id, nameof(id));
            var statusRoute = new Uri(
                string.Format(CultureInfo.InvariantCulture, _config.StatusRouteTemplate, id),
                UriKind.Relative);

            using (HttpResponseMessage response = await _client.GetAsync(statusRoute, cancellationToken))
            {
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
}
