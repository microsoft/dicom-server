// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Operations;
using Polly;
using Polly.Retry;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Extensions;

internal static class DicomWebClientExtensions
{
    private static readonly AsyncRetryPolicy<OperationState<DicomOperation>> OperationPolicy = Policy
       .HandleResult<OperationState<DicomOperation>>(x => x.Status.IsInProgress())
       .WaitAndRetryAsync(100, x => TimeSpan.FromSeconds(3)); // Retry 100 times and wait for 3 seconds after each retry

    public static Task<OperationState<DicomOperation>> WaitForCompletionAsync(this IDicomWebClient client, Guid operationId, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        return OperationPolicy.ExecuteAsync(
            async t =>
            {
                DicomWebResponse<OperationState<DicomOperation>> response = await client.GetOperationStateAsync(operationId, t);
                return await response.GetValueAsync();
            },
            cancellationToken);
    }
}
