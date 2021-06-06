// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Core.Model.Operations;
using Microsoft.Health.Dicom.Core.Operations;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Features.Operations
{
    public class DicomDurableFunctionsHttpClientService : IDicomOperationsHttpClientService
    {
        private readonly HttpClient _client;
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        };

        public DicomDurableFunctionsHttpClientService(HttpClient client, OperationsConfiguration configuration)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(configuration?.BaseAddress, nameof(configuration));

            client.BaseAddress = configuration.BaseAddress;
            client.DefaultRequestHeaders.Add(HeaderNames.Accept, MediaTypeNames.Application.Json);
            _client = client;
        }

        public async Task<OperationStatusResponse> GetStatusAsync(string id, CancellationToken cancellationToken = default)
        {
            var jobPath = new Uri("/Jobs/" + id, UriKind.Relative);
            using (HttpResponseMessage response = await _client.GetAsync(jobPath, cancellationToken))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new OperationStatusResponse();
                }

                // Re-throw any exceptions we may have encountered when making the HTTP request
                response.EnsureSuccessStatusCode();

                DurableOrchestrationInstanceState responseState = JsonConvert.DeserializeObject<DurableOrchestrationInstanceState>(
#if NET5_0_OR_GREATER
                await response.Content.ReadAsStringAsync(cancellationToken),
#else
                await response.Content.ReadAsStringAsync(),
#endif
                    JsonSettings);

                OperationStatus status = GetOperationStatus(responseState.RuntimeStatus);
                return new OperationStatusResponse(
                    responseState.InstanceId,
                    GetOperationType(responseState.Name),
                    responseState.CreatedTime,
                    status,
                    status == OperationStatus.Failed ? responseState.CustomStatus.ToObject<string>() : null);
            }
        }

        private static OperationStatus GetOperationStatus(string status)
        {
            return status switch
            {
                "Pending" => OperationStatus.Pending,
                "Running" or "ContinuedAsNew" => OperationStatus.Running,
                "Completed" => OperationStatus.Completed,
                "Failed" => OperationStatus.Failed,
                "Canceled" or "Terminated" => OperationStatus.Canceled,
                _ => OperationStatus.Unknown,
            };
        }

        private static OperationType GetOperationType(string name)
        {
            return string.Equals(name, "AddQueryTag", StringComparison.OrdinalIgnoreCase)
                ? OperationType.AddExtendedQueryTag
                : OperationType.Unknown;
        }
    }
}
