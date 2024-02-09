// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Operations;
using Polly;
using Polly.Retry;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

public class DeleteExtendedQueryTagService : IDeleteExtendedQueryTagService
{
    private readonly IExtendedQueryTagStore _extendedQueryTagStore;
    private readonly IDicomTagParser _dicomTagParser;
    private readonly IGuidFactory _guidFactory;
    private readonly IDicomOperationsClient _client;
    private readonly ExtendedQueryTagConfiguration _extendedQueryTagConfiguration;
    private readonly ILogger<DeleteExtendedQueryTagService> _logger;
    private readonly AsyncRetryPolicy<OperationStatus> _retryPolicy;

    public DeleteExtendedQueryTagService(
        IExtendedQueryTagStore extendedQueryTagStore,
        IDicomTagParser dicomTagParser,
        IGuidFactory guidFactory,
        IDicomOperationsClient client,
        IOptions<ExtendedQueryTagConfiguration> options,
        ILogger<DeleteExtendedQueryTagService> logger)
    {
        EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
        EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(guidFactory, nameof(guidFactory));
        EnsureArg.IsNotNull(options?.Value, nameof(options));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _extendedQueryTagStore = extendedQueryTagStore;
        _dicomTagParser = dicomTagParser;
        _client = client;
        _guidFactory = guidFactory;
        _extendedQueryTagConfiguration = options.Value;
        _logger = logger;

        // 90 retries * 10 seconds = 15 minutes total wait time
        _retryPolicy = Policy
            .HandleResult<OperationStatus>(s => s.IsInProgress())
            .WaitAndRetryAsync(
                _extendedQueryTagConfiguration.OperationRetryCount,
                retryAttempt => _extendedQueryTagConfiguration.OperationRetryInterval);
    }

    public async Task DeleteExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken)
    {
        DicomTag[] tags;
        if (!_dicomTagParser.TryParse(tagPath, out tags))
        {
            throw new InvalidExtendedQueryTagPathException(
                string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidExtendedQueryTag, tagPath ?? string.Empty));
        }

        string normalizedPath = tags[0].GetPath();

        // Get tag and update status to deleting. If the key is not found or the status is already deleting, it will throw an exception.
        ExtendedQueryTagStoreEntry xqt = await _extendedQueryTagStore.GetExtendedQueryTagAsync(normalizedPath, cancellationToken);
        await _extendedQueryTagStore.UpdateExtendedQueryTagStatusToDelete(xqt.Key, cancellationToken);

        // Start operation to delete extended query tag
        Guid operationId = _guidFactory.Create();
        await _client.StartDeleteExtendedQueryTagOperationAsync(operationId, xqt.Key, xqt.VR, cancellationToken);

        // Wait for operation to finish to keep the API synchronous
        OperationStatus finalStatus = await _retryPolicy.ExecuteAsync(async (ct) =>
        {
            IOperationState<DicomOperation> state = await _client.GetStateAsync(operationId, cancellationToken);

            _logger.LogInformation(
                "Waiting for the operation to delete extended query tag to complete. Status: {Status}",
                state.Status);

            return state.Status;
        },
        cancellationToken);

        // If the operation is still in progress, or if it did not complete successfully, throw an exception
        if (finalStatus.IsInProgress())
        {
            throw new DataStoreException($"The operation to delete extended query tags failed to complete in the alloted time. Status: {finalStatus}");
        }

        if (finalStatus is OperationStatus.Canceled or OperationStatus.Failed)
        {
            throw new DataStoreException($"The operation to delete extended query tags failed to complete. Status: {finalStatus}");
        }
    }
}
